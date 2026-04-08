namespace TukiFact.Domain.Entities;

public class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WebhookConfigId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}"; // JSON payload sent
    public string? ResponseStatus { get; set; } // HTTP status code
    public string? ResponseBody { get; set; }
    public int Attempt { get; set; } = 1;
    public string Status { get; set; } = "pending"; // pending, delivered, failed
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public WebhookConfig WebhookConfig { get; set; } = null!;
}
