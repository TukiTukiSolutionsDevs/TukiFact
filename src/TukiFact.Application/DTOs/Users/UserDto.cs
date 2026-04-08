namespace TukiFact.Application.DTOs.Users;

public record CreateUserRequest(
    string Email,
    string Password,
    string FullName,
    string Role = "emisor"
);

public record UpdateUserRequest(
    string? FullName,
    string? Role,
    bool? IsActive
);

public record UserResponse(
    Guid Id,
    string Email,
    string? FullName,
    string Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt
);
