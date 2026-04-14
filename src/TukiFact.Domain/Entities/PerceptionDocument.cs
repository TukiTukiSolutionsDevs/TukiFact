namespace TukiFact.Domain.Entities;

public class PerceptionDocument
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Document identification
    public string DocumentType { get; set; } = "40"; // 40=Percepción
    public string Serie { get; set; } = string.Empty; // P001, P002
    public long Correlative { get; set; }
    public string FullNumber => $"{Serie}-{Correlative:D8}";

    // Dates
    public DateOnly IssueDate { get; set; }

    // Customer (comprador — a quien se cobra la percepción)
    public string CustomerDocType { get; set; } = "6"; // 6=RUC
    public string CustomerDocNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }

    // Perception regime (Catálogo 22)
    public string RegimeCode { get; set; } = "01"; // 01=Venta interna 2%, 02=Combustible 1%, 03=CdP 0.5%
    public decimal PerceptionPercent { get; set; } = 2.00m;

    // Totals
    public decimal TotalInvoiceAmount { get; set; }    // Total cobrado (antes de percepción)
    public decimal TotalPerceived { get; set; }         // Total percibido
    public decimal TotalCollected { get; set; }         // Total cobrado al comprador (invoice + percepción)
    public string Currency { get; set; } = "PEN";

    // Observations
    public string? Notes { get; set; }

    // SUNAT
    public string Status { get; set; } = "draft";
    public string? SunatResponseCode { get; set; }
    public string? SunatResponseDescription { get; set; }
    public string? HashCode { get; set; }

    // Files (MinIO paths)
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? CdrUrl { get; set; }

    // Audit
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<PerceptionDocumentReference> References { get; set; } = new List<PerceptionDocumentReference>();
}

public class PerceptionDocumentReference
{
    public Guid Id { get; set; }
    public Guid PerceptionDocumentId { get; set; }

    // Referenced document (factura emitida al comprador)
    public string DocumentType { get; set; } = "01"; // 01=Factura
    public string DocumentNumber { get; set; } = string.Empty;
    public DateOnly DocumentDate { get; set; }
    public decimal InvoiceAmount { get; set; }
    public string InvoiceCurrency { get; set; } = "PEN";

    // Collection info
    public DateOnly CollectionDate { get; set; }
    public int CollectionNumber { get; set; } = 1;
    public decimal CollectionAmount { get; set; }

    // Perception calculated
    public decimal PerceivedAmount { get; set; }
    public decimal TotalCollectedAmount { get; set; }  // cobro + percepción

    // Exchange rate (if invoice in USD)
    public decimal? ExchangeRate { get; set; }
    public DateOnly? ExchangeRateDate { get; set; }

    // Navigation
    public PerceptionDocument PerceptionDocument { get; set; } = null!;
}
