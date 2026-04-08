namespace TukiFact.Domain.Entities;

public class WebhookConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty; // HMAC secret for signature
    public string Events { get; set; } = "[]"; // JSON array: ["document.created","document.accepted","document.rejected","document.voided"]
    public bool IsActive { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public DateTimeOffset? LastTriggeredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
