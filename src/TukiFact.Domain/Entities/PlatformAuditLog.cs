namespace TukiFact.Domain.Entities;

/// <summary>
/// Platform-level audit log for backoffice actions.
/// Tracks: impersonation, tenant changes, employee changes, config changes.
/// Different from AuditLog which is tenant-scoped.
/// </summary>
public class PlatformAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? PlatformUserId { get; set; }
    public string Action { get; set; } = string.Empty; // tenant.suspended, tenant.activated, employee.created, config.updated, tenant.impersonated
    public string EntityType { get; set; } = string.Empty; // Tenant, PlatformUser, PlatformConfig
    public Guid? EntityId { get; set; }
    public string? Details { get; set; } // JSON
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public PlatformUser? PlatformUser { get; set; }
}
