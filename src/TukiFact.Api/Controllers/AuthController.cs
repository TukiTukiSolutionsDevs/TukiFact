using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Auth;
using TukiFact.Application.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, ct);
            _logger.LogInformation("New tenant registered: RUC {Ruc}", request.Ruc);
            return Created($"/v1/company", result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.LoginAsync(request, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Request password reset email.
    /// Always returns 200 (even if email not found) to prevent enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.ForgotPasswordAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ForgotPassword error for {Email}", request.Email);
        }
        // Always return OK to prevent email enumeration
        return Ok(new { message = "Si el email existe, recibirás un enlace para restablecer tu contraseña." });
    }

    /// <summary>
    /// Reset password using token from email.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.ResetPasswordAsync(request, ct);
            return Ok(new { message = "Contraseña restablecida exitosamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Me()
    {
        // ASP.NET JWT middleware remaps standard claims:
        //   JWT "sub"   → ClaimTypes.NameIdentifier
        //   JWT "email" → ClaimTypes.Email
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new { userId, tenantId, email, role });
    }
}
