using TukiFact.Application.DTOs.Auth;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        ITenantRepository tenantRepo,
        IUserRepository userRepo,
        IPlanRepository planRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IJwtService jwtService,
        IPasswordHasher passwordHasher)
    {
        _tenantRepo = tenantRepo;
        _userRepo = userRepo;
        _planRepo = planRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // Check if RUC already exists
        var existing = await _tenantRepo.GetByRucAsync(request.Ruc, ct);
        if (existing is not null)
            throw new InvalidOperationException($"RUC {request.Ruc} ya está registrado");

        // Get free plan
        var freePlan = await _planRepo.GetByNameAsync("Free", ct);

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
}
