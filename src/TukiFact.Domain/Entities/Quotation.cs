namespace TukiFact.Domain.Entities;

public class Quotation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Identification
    public string QuotationNumber { get; set; } = string.Empty; // COT-001, COT-002
    public long Correlative { get; set; }

    // Dates
    public DateOnly IssueDate { get; set; }
    public DateOnly ValidUntil { get; set; }

    // Customer
    public string CustomerDocType { get; set; } = "6";
    public string CustomerDocNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    // Currency
    public string Currency { get; set; } = "PEN";

    // Amounts (calculated from items)
    public decimal Subtotal { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }
    public decimal TotalDiscount { get; set; }

    // Status: draft → sent → approved → invoiced → cancelled → expired
    public string Status { get; set; } = "draft";

    // Converted to invoice
    public Guid? InvoiceDocumentId { get; set; }
    public string? InvoiceDocumentNumber { get; set; }

    // Notes / Terms
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }

    // Files
    public string? PdfUrl { get; set; }

    // Audit
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Document? InvoiceDocument { get; set; }
    public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
}

public class QuotationItem
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }

    public int Sequence { get; set; }
    public string? ProductCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitMeasure { get; set; } = "NIU";
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public string IgvType { get; set; } = "10"; // 10=Gravado
    public decimal IgvAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }

    // Navigation
    public Quotation Quotation { get; set; } = null!;
}
