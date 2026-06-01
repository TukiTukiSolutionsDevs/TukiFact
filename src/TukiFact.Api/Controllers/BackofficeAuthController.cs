using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Infrastructure.Persistence;
using TukiFact.Infrastructure.Services;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/backoffice/auth")]
public class BackofficeAuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtService _jwtService;
    private readonly IGoogleAuthService _googleAuth;
    private readonly ILogger<BackofficeAuthController> _logger;

    public BackofficeAuthController(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IGoogleAuthService googleAuth,
        ILogger<BackofficeAuthController> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = (JwtService)jwtService;
        _googleAuth = googleAuth;
        _logger = logger;
    }

    public record BackofficeLoginRequest(string Email, string Password);
    public record BackofficeGoogleLoginRequest(string IdToken);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] BackofficeLoginRequest request, CancellationToken ct)
    {
        var user = await _db.PlatformUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, ct);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Credenciales inválidas" });
        }

        var accessToken = _jwtService.GeneratePlatformAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Backoffice login: {Email} ({Role})", user.Email, user.Role);

        return Ok(new
        {
            accessToken,
            refreshToken,
            expiresAt = DateTime.UtcNow.AddMinutes(60),
            user = new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                role = user.Role
            }
        });
    }

    [HttpPost("google")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] BackofficeGoogleLoginRequest request, CancellationToken ct)
    {
        GoogleUserInfo googleUser;
        try
        {
            googleUser = await _googleAuth.ValidateIdTokenAsync(request.IdToken, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }

        var user = await _db.PlatformUsers
            .FirstOrDefaultAsync(u => u.Email == googleUser.Email && u.IsActive, ct);

        if (user is null)
        {
            _logger.LogWarning("Backoffice Google login denied for unknown email: {Email}", googleUser.Email);
            return Unauthorized(new { error = "Tu cuenta no está autorizada para el backoffice" });
        }

        var accessToken = _jwtService.GeneratePlatformAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Backoffice Google login: {Email} ({Role})", user.Email, user.Role);

        return Ok(new
        {
            accessToken,
            refreshToken,
            expiresAt = DateTime.UtcNow.AddMinutes(60),
            user = new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                role = user.Role
            }
        });
    }
}
