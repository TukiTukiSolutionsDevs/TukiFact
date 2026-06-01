namespace TukiFact.Application.DTOs.Auth;

public record TenantChoice(Guid TenantId, string Ruc, string RazonSocial);

public record GoogleRegistrationPrompt(string Email, string Name, string? Picture);

/// <summary>
/// Response from Google login attempt.
/// If <see cref="Auth"/> is set → user is logged in.
/// If <see cref="Tenants"/> has items → user must pick which tenant to log into.
/// If <see cref="NeedsRegistration"/> is set → the Google email has no tenant yet;
/// the frontend should walk the user through /register with the Google token.
/// </summary>
public record GoogleLoginResult(
    AuthResponse? Auth,
    IReadOnlyList<TenantChoice>? Tenants,
    GoogleRegistrationPrompt? NeedsRegistration = null);
