namespace TukiFact.Domain.Entities;

/// <summary>
/// Captured request/response for an `Idempotency-Key` header on emit endpoints.
/// Replayed on duplicate POSTs so a retry never double-emits to SUNAT.
/// </summary>
public class IdempotencyKey
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; } // null for anonymous endpoints (none today, future-proof)
    public string Key { get; set; } = string.Empty; // composite: {Path}:{header}
    public string RequestHash { get; set; } = string.Empty; // SHA-256 of body
    public string Endpoint { get; set; } = string.Empty;
    public int ResponseStatus { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(24);
}
