namespace TukiFact.Application.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserInfo User
);

public record UserInfo(
    Guid Id,
    Guid TenantId,
    string Email,
    string? FullName,
    string Role
);
