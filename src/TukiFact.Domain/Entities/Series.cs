namespace TukiFact.Domain.Entities;

public class Series
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string DocumentType { get; set; } = string.Empty; // 01, 03, 07, 08, RC, RA
    public string Serie { get; set; } = string.Empty; // F001, B001, etc.
    public long CurrentCorrelative { get; set; } = 0;
    public string EmissionPoint { get; set; } = "PRINCIPAL";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
