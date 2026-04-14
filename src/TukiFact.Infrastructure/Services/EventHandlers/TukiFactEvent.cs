namespace TukiFact.Infrastructure.Services.EventHandlers;

/// <summary>
/// Standard event payload published to NATS JetStream.
/// All event handlers deserialize from this shape.
/// </summary>
public class TukiFactEvent
{
    public Guid TenantId { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty; // Document, Quotation, Retention, etc.
    public string EventType { get; set; } = string.Empty;  // document.sent, document.created, etc.
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // Document-specific fields (optional, only populated for document events)
    public string? DocumentType { get; set; }     // 01, 03, 07, 08
    public string? Serie { get; set; }            // F001
    public long? Correlative { get; set; }        // 123
    public string? FullNumber { get; set; }       // F001-00000123
    public decimal? Total { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? SunatResponseCode { get; set; }
    public string? SunatResponseDescription { get; set; }
    public string? ErrorMessage { get; set; }
}
