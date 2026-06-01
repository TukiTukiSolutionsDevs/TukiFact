using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TukiFact.Application.DTOs.Auth;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IGoogleAuthService _googleAuth;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;
    private readonly string _frontendUrl;

    public AuthService(
        ITenantRepository tenantRepo,
        IUserRepository userRepo,
        IPlanRepository planRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IGoogleAuthService googleAuth,
        AppDbContext dbContext,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _tenantRepo = tenantRepo;
        _userRepo = userRepo;
        _planRepo = planRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _googleAuth = googleAuth;
        _dbContext = dbContext;
        _logger = logger;
        _frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:3000";
    }

    public async Task<GoogleLoginResult> LoginWithGoogleAsync(LoginWithGoogleRequest request, CancellationToken ct = default)
    {
        var googleUser = await _googleAuth.ValidateIdTokenAsync(request.IdToken, ct);

        // If client already picked a tenant → log into it directly.
        if (request.TenantId is Guid tid)
        {
            var picked = await _userRepo.GetByEmailAsync(googleUser.Email, tid, ct)
                ?? throw new UnauthorizedAccessException("Tu cuenta de Google no está registrada en esa empresa");

            if (!picked.IsActive)
                throw new UnauthorizedAccessException("Usuario desactivado");

            var auth = await IssueTokensAsync(picked, ct);
            return new GoogleLoginResult(auth, null);
        }

        // No tenant chosen → list active matches across tenants.
        var matches = await _userRepo.GetActiveByEmailWithTenantsAsync(googleUser.Email, ct);

        if (matches.Count == 0)
            throw new UnauthorizedAccessException(
                "Tu correo no está registrado en ninguna empresa. Pide a tu administrador que te invite, o registra una nueva empresa.");

        if (matches.Count == 1)
        {
            var auth = await IssueTokensAsync(matches[0], ct);
            return new GoogleLoginResult(auth, null);
        }

        // Multiple → user must pick.
        var options = matches
            .Select(u => new TenantChoice(u.Tenant.Id, u.Tenant.Ruc, u.Tenant.RazonSocial))
            .ToList();
        return new GoogleLoginResult(null, options);
    }

    public async Task<AuthResponse> RegisterWithGoogleAsync(RegisterWithGoogleRequest request, CancellationToken ct = default)
    {
        var googleUser = await _googleAuth.ValidateIdTokenAsync(request.IdToken, ct);

        var existingTenant = await _tenantRepo.GetByRucAsync(request.Ruc, ct);
        if (existingTenant is not null)
            throw new InvalidOperationException($"RUC {request.Ruc} ya está registrado");

        // If a User already exists with this Google email anywhere, prevent collisions.
        var alreadyUser = await _userRepo.GetByEmailGlobalAsync(googleUser.Email, ct);
        if (alreadyUser is not null)
            throw new InvalidOperationException(
                "Tu cuenta de Google ya está vinculada a otra empresa. Inicia sesión en su lugar.");

        var freePlan = await _planRepo.GetByNameAsync("Gratis", ct);

        var tenant = new Tenant
        {
            Ruc = request.Ruc,
            RazonSocial = request.RazonSocial,
            NombreComercial = request.NombreComercial,
            Direccion = request.Direccion,
            PlanId = freePlan?.Id,
            Environment = "beta"
        };
        await _tenantRepo.CreateAsync(tenant, ct);

        // No password — Google is the source of truth. Store a structurally-valid but
        // unusable BCrypt hash so password verification can never succeed.
        var disabledHash = $"$2a$11${Guid.NewGuid():N}{Guid.NewGuid():N}".Substring(0, 60);

        var user = new User
        {
            TenantId = tenant.Id,
            Email = googleUser.Email,
            PasswordHash = disabledHash,
            FullName = googleUser.Name ?? googleUser.Email,
            Role = "admin"
        };
        await _userRepo.CreateAsync(user, ct);

        var auth = await IssueTokensAsync(user, ct);
        _logger.LogInformation("Google register: tenant={Ruc} admin={Email}", tenant.Ruc, user.Email);
        return auth;
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userRepo.UpdateAsync(user, ct);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        await _refreshTokenRepo.CreateAsync(refreshToken, ct);

        return new AuthResponse(
            accessToken,
            refreshTokenValue,
            DateTimeOffset.UtcNow.AddMinutes(60),
            new UserInfo(user.Id, user.TenantId, user.Email, user.FullName, user.Role));
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // Check if RUC already exists
        var existing = await _tenantRepo.GetByRucAsync(request.Ruc, ct);
        if (existing is not null)
            throw new InvalidOperationException($"RUC {request.Ruc} ya está registrado");

        // Get free plan
        var freePlan = await _planRepo.GetByNameAsync("Gratis", ct);

        // Create tenant
        var tenant = new Tenant
        {
            Ruc = request.Ruc,
            RazonSocial = request.RazonSocial,
            NombreComercial = request.NombreComercial,
            Direccion = request.Direccion,
            PlanId = freePlan?.Id,
            Environment = "beta"
        };
        await _tenantRepo.CreateAsync(tenant, ct);

        // Create admin user
        var user = new User
        {
            TenantId = tenant.Id,
            Email = request.AdminEmail,
            PasswordHash = _passwordHasher.Hash(request.AdminPassword),
            FullName = request.AdminFullName,
            Role = "admin"
        };
        await _userRepo.CreateAsync(user, ct);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TenantId = tenant.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        await _refreshTokenRepo.CreateAsync(refreshToken, ct);

        return new AuthResponse(
            accessToken,
            refreshTokenValue,
            DateTimeOffset.UtcNow.AddMinutes(60),
            new UserInfo(user.Id, tenant.Id, user.Email, user.FullName, user.Role));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email, request.TenantId, ct)
            ?? throw new UnauthorizedAccessException("Credenciales inválidas");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Usuario desactivado");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas");

        // Update last login
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userRepo.UpdateAsync(user, ct);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        await _refreshTokenRepo.CreateAsync(refreshToken, ct);

        return new AuthResponse(
            accessToken,
            refreshTokenValue,
            DateTimeOffset.UtcNow.AddMinutes(60),
            new UserInfo(user.Id, user.TenantId, user.Email, user.FullName, user.Role));
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var storedToken = await _refreshTokenRepo.GetByTokenAsync(request.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException("Refresh token inválido");

        if (!storedToken.IsActive)
            throw new UnauthorizedAccessException("Refresh token expirado o revocado");

        // Revoke old token
        await _refreshTokenRepo.RevokeAsync(storedToken.Id, ct);

        var user = storedToken.User;

        // Generate new tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshTokenValue = _jwtService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        await _refreshTokenRepo.CreateAsync(newRefreshToken, ct);

        return new AuthResponse(
            accessToken,
            newRefreshTokenValue,
            DateTimeOffset.UtcNow.AddMinutes(60),
            new UserInfo(user.Id, user.TenantId, user.Email, user.FullName, user.Role));
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailGlobalAsync(request.Email, ct);
        if (user is null)
        {
            _logger.LogWarning("ForgotPassword: email {Email} not found (silent)", request.Email);
            return; // Silent — don't reveal if email exists
        }

        // Generate secure token
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var tokenStr = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = tokenStr,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) // 1 hour expiry
        };

        await _dbContext.PasswordResetTokens.AddAsync(resetToken, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Send reset email
        var resetUrl = $"{_frontendUrl}/reset-password?token={tokenStr}";
        var emailMessage = new EmailMessage
        {
            To = user.Email,
            Subject = "Restablecer contraseña — TukiFact",
            Template = "reset_password",
            TenantId = user.TenantId,
            HtmlBody = $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"></head>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
                <div style="background: #1a1a2e; border-radius: 8px; padding: 24px; margin-bottom: 20px; text-align: center;">
                    <h2 style="margin: 0; color: #fff;">TukiFact</h2>
                </div>

                <div style="padding: 20px 0;">
                    <p>Hola <strong>{user.FullName}</strong>,</p>
                    <p>Recibimos una solicitud para restablecer tu contraseña. Hacé clic en el siguiente botón:</p>

                    <div style="text-align: center; margin: 30px 0;">
                        <a href="{resetUrl}" 
                           style="background: #4f46e5; color: white; padding: 14px 28px; border-radius: 8px; text-decoration: none; font-weight: bold; display: inline-block;">
                            Restablecer Contraseña
                        </a>
                    </div>

                    <p style="color: #666; font-size: 14px;">Este enlace expira en <strong>1 hora</strong>.</p>
                    <p style="color: #666; font-size: 14px;">Si no solicitaste esto, ignorá este email.</p>
                </div>

                <div style="border-top: 1px solid #eee; padding-top: 16px; margin-top: 20px;">
                    <p style="color: #999; font-size: 12px;">
                        Este es un email automático de TukiFact. No responder.
                    </p>
                </div>
            </body>
            </html>
            """
        };

        await _emailService.SendAsync(emailMessage, ct);
        _logger.LogInformation("Password reset email sent to {Email}", user.Email);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var resetToken = _dbContext.PasswordResetTokens
            .FirstOrDefault(t => t.Token == request.Token);

        if (resetToken is null)
            throw new InvalidOperationException("Token inválido o no encontrado.");

        if (resetToken.IsExpired)
            throw new InvalidOperationException("El token ha expirado. Solicitá uno nuevo.");

        if (resetToken.IsUsed)
            throw new InvalidOperationException("Este token ya fue utilizado.");

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
            throw new InvalidOperationException("La contraseña debe tener al menos 6 caracteres.");

        // Update password
        var user = await _userRepo.GetByIdAsync(resetToken.UserId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _userRepo.UpdateAsync(user, ct);

        // Mark token as used
        resetToken.UsedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
    }
}
