namespace TukiFact.Application.DTOs.Quotations;

public record CreateQuotationRequest(
    DateOnly? IssueDate,
    DateOnly ValidUntil,
    string CustomerDocType,
    string CustomerDocNumber,
    string CustomerName,
    string? CustomerAddress,
    string? CustomerEmail,
    string? CustomerPhone,
    string Currency,
    string? Notes,
    string? TermsAndConditions,
    List<CreateQuotationItemRequest> Items
);

public record CreateQuotationItemRequest(
    string? ProductCode,
    string Description,
    decimal Quantity,
    string UnitMeasure,
    decimal UnitPrice,
    string IgvType,
    decimal Discount = 0
);

public record QuotationResponse(
    Guid Id,
    string QuotationNumber,
    long Correlative,
    DateOnly IssueDate,
    DateOnly ValidUntil,
    string CustomerDocType,
    string CustomerDocNumber,
    string CustomerName,
    string? CustomerEmail,
    string Currency,
    decimal Subtotal,
    decimal Igv,
    decimal Total,
    string Status,
    Guid? InvoiceDocumentId,
    string? InvoiceDocumentNumber,
    string? PdfUrl,
    string? Notes,
    DateTimeOffset CreatedAt,
    List<QuotationItemResponse> Items
);

public record QuotationItemResponse(
    int Sequence,
    string? ProductCode,
    string Description,
    decimal Quantity,
    string UnitMeasure,
    decimal UnitPrice,
    string IgvType,
    decimal IgvAmount,
    decimal Subtotal,
    decimal Total
);
