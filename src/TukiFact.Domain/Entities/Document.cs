namespace TukiFact.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Document identification
    public string DocumentType { get; set; } = "01"; // 01=Factura, 03=Boleta, 07=NC, 08=ND
    public Guid SeriesId { get; set; }
    public string Serie { get; set; } = string.Empty;     // F001, B001
    public long Correlative { get; set; }                   // 1, 2, 3...
    public string FullNumber => $"{Serie}-{Correlative:D8}"; // F001-00000001

    // Dates
    public DateOnly IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }

    // Currency
    public string Currency { get; set; } = "PEN"; // PEN, USD

    // Customer (receptor)
    public string CustomerDocType { get; set; } = "6"; // 6=RUC, 1=DNI, 0=Sin doc
    public string CustomerDocNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }

    // Amounts
    public decimal OperacionGravada { get; set; }   // Subtotal gravado
    public decimal OperacionExonerada { get; set; }  // Subtotal exonerado
    public decimal OperacionInafecta { get; set; }   // Subtotal inafecto
    public decimal OperacionGratuita { get; set; }   // Operaciones gratuitas
    public decimal Igv { get; set; }                  // 18% del gravado
    public decimal TotalDescuento { get; set; }
    public decimal Total { get; set; }

    // SUNAT
    public string Status { get; set; } = "draft"; // draft, signed, sent, accepted, rejected, voided, observed
    public string? XmlUrl { get; set; }       // MinIO path
    public string? PdfUrl { get; set; }       // MinIO path
    public string? CdrUrl { get; set; }       // MinIO path
    public string? SunatResponseCode { get; set; }
    public string? SunatResponseDescription { get; set; }
    public string? HashCode { get; set; }     // digest value from XML signature
    public string? QrData { get; set; }

    // Metadata
    public string? Notes { get; set; }
    public string? PurchaseOrder { get; set; }

    // Reference document (for NC/ND)
    public Guid? ReferenceDocumentId { get; set; }
    public string? ReferenceDocumentType { get; set; }  // 01, 03
    public string? ReferenceDocumentNumber { get; set; } // F001-00000001
    public string? CreditNoteReason { get; set; }  // Código motivo (catálogo 09 SUNAT)
    public string? DebitNoteReason { get; set; }   // Código motivo (catálogo 10 SUNAT)

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Series SeriesNav { get; set; } = null!;
    public ICollection<DocumentItem> Items { get; set; } = new List<DocumentItem>();
    public ICollection<DocumentXmlLog> XmlLogs { get; set; } = new List<DocumentXmlLog>();
    public Document? ReferenceDocument { get; set; }
}
