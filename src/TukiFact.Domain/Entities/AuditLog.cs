namespace TukiFact.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty; // document.created, user.login, series.created, apikey.generated, etc.
    public string EntityType { get; set; } = string.Empty; // Document, User, Series, ApiKey, etc.
    public Guid? EntityId { get; set; }
    public string? Details { get; set; } // JSON with action-specific details
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
