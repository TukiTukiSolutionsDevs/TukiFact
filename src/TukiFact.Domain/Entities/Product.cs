namespace TukiFact.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Identification
    public string Code { get; set; } = string.Empty;           // Internal code (SKU)
    public string? SunatCode { get; set; }                     // SUNAT product code (catálogo 25)
    public string Description { get; set; } = string.Empty;

    // Pricing
    public decimal UnitPrice { get; set; }                     // Price WITHOUT IGV
    public decimal UnitPriceWithIgv { get; set; }              // Price WITH IGV
    public string Currency { get; set; } = "PEN";

    // Tax
    public string IgvType { get; set; } = "10";               // 10=Gravado, 20=Exonerado, 30=Inafecto
    public string UnitMeasure { get; set; } = "NIU";           // NIU=Unidad, ZZ=Servicio, etc.

    // Classification
    public string? Category { get; set; }
    public string? Brand { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Metadata
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
