namespace TukiFact.Domain.Entities;

/// <summary>
/// In-app notification for users. Created by NATS event handlers.
/// Delivered in real-time via SSE (Server-Sent Events).
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }  // null = for all users in the tenant
    public string Type { get; set; } = string.Empty;    // document.sent, document.failed, etc.
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? EntityType { get; set; }  // Document, Quotation, Retention, etc.
    public Guid? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
