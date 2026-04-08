namespace TukiFact.Application.DTOs.AuditLog;

public record AuditLogResponse(
    Guid Id, string Action, string EntityType, Guid? EntityId,
    string? Details, Guid? UserId, string? IpAddress,
    DateTimeOffset CreatedAt
);
