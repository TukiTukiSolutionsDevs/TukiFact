namespace TukiFact.Application.DTOs.Retentions;

public record CreateRetentionRequest(
    string Serie, // R001
    DateOnly? IssueDate,
    string SupplierDocType,
    string SupplierDocNumber,
    string SupplierName,
    string? SupplierAddress,
    string RegimeCode, // "01" = Tasa 3%
    decimal RetentionPercent, // 3.00
    string Currency,
    string? Notes,
    List<CreateRetentionReferenceRequest> References
);

public record CreateRetentionReferenceRequest(
    string DocumentType, // "01" = Factura
    string DocumentNumber, // F001-00000100
    DateOnly DocumentDate,
    decimal InvoiceAmount,
    string InvoiceCurrency,
    DateOnly PaymentDate,
    int PaymentNumber,
    decimal PaymentAmount,
    decimal? ExchangeRate,
    DateOnly? ExchangeRateDate
);

public record RetentionResponse(
    Guid Id,
    string Serie,
    long Correlative,
    string FullNumber,
    DateOnly IssueDate,
    string SupplierDocType,
    string SupplierDocNumber,
    string SupplierName,
    string RegimeCode,
    decimal RetentionPercent,
    decimal TotalInvoiceAmount,
    decimal TotalRetained,
    decimal TotalPaid,
    string Currency,
    string Status,
    string? SunatResponseCode,
    string? SunatResponseDescription,
    string? XmlUrl,
    string? PdfUrl,
    DateTimeOffset CreatedAt,
    List<RetentionReferenceResponse> References
);

public record RetentionReferenceResponse(
    Guid Id,
    string DocumentType,
    string DocumentNumber,
    DateOnly DocumentDate,
    decimal InvoiceAmount,
    DateOnly PaymentDate,
    decimal PaymentAmount,
    decimal RetainedAmount,
    decimal NetPaidAmount
);
