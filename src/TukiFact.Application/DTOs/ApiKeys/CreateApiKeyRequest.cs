namespace TukiFact.Application.DTOs.ApiKeys;

public record CreateApiKeyRequest(string Name, string[] Permissions);

public record ApiKeyResponse(
    Guid Id,
    string KeyPrefix,
    string? Name,
    string[] Permissions,
    bool IsActive,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset CreatedAt,
    string? PlainTextKey = null  // Only returned on creation
);
