namespace TukiFact.Domain.Entities;

public class RetentionDocument
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Document identification
    public string DocumentType { get; set; } = "20"; // 20=Retención
    public string Serie { get; set; } = string.Empty; // R001, R002
    public long Correlative { get; set; }
    public string FullNumber => $"{Serie}-{Correlative:D8}";

    // Dates
    public DateOnly IssueDate { get; set; }

    // Supplier (proveedor — a quien se retiene)
    public string SupplierDocType { get; set; } = "6"; // 6=RUC
    public string SupplierDocNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierAddress { get; set; }

    // Retention regime (Catálogo 23)
    public string RegimeCode { get; set; } = "01"; // 01=Tasa 3%
    public decimal RetentionPercent { get; set; } = 3.00m;

    // Totals
    public decimal TotalInvoiceAmount { get; set; }  // Total pagado (antes de retención)
    public decimal TotalRetained { get; set; }        // Total retenido
    public decimal TotalPaid { get; set; }            // Total neto pagado al proveedor
    public string Currency { get; set; } = "PEN";

    // Observations
    public string? Notes { get; set; }

    // SUNAT
    public string Status { get; set; } = "draft"; // draft, signed, sent, accepted, rejected, voided
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
    public ICollection<RetentionDocumentReference> References { get; set; } = new List<RetentionDocumentReference>();
}

public class RetentionDocumentReference
{
    public Guid Id { get; set; }
    public Guid RetentionDocumentId { get; set; }

    // Referenced document (factura del proveedor)
    public string DocumentType { get; set; } = "01"; // 01=Factura
    public string DocumentNumber { get; set; } = string.Empty; // F001-00000100
    public DateOnly DocumentDate { get; set; }
    public decimal InvoiceAmount { get; set; }       // Monto de la factura
    public string InvoiceCurrency { get; set; } = "PEN";

    // Payment info
    public DateOnly PaymentDate { get; set; }
    public int PaymentNumber { get; set; } = 1;      // Número de pago
    public decimal PaymentAmount { get; set; }        // Monto pagado

    // Retention calculated
    public decimal RetainedAmount { get; set; }       // Monto retenido (3% del pago)
    public decimal NetPaidAmount { get; set; }        // Neto pagado al proveedor

    // Exchange rate (if invoice in USD)
    public decimal? ExchangeRate { get; set; }
    public DateOnly? ExchangeRateDate { get; set; }

    // Navigation
    public RetentionDocument RetentionDocument { get; set; } = null!;
}
