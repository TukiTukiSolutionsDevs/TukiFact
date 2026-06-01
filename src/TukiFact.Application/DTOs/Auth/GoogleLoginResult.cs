namespace TukiFact.Application.DTOs.Auth;

public record TenantChoice(Guid TenantId, string Ruc, string RazonSocial);

/// <summary>
/// Response from Google login attempt.
/// If <see cref="Auth"/> is set → user is logged in.
/// If <see cref="Tenants"/> has items → user must pick which tenant to log into.
/// </summary>
public record GoogleLoginResult(
    AuthResponse? Auth,
    IReadOnlyList<TenantChoice>? Tenants);
