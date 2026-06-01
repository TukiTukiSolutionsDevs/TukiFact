namespace TukiFact.Application.Interfaces;

public record GoogleUserInfo(string Email, string? Name, string? Picture, string Subject);

public interface IGoogleAuthService
{
    /// <summary>
    /// Validates a Google ID token against the configured audience and returns the verified user info.
    /// Throws UnauthorizedAccessException if invalid.
    /// </summary>
    Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}
