namespace TukiFact.Application.DTOs.Perceptions;

public record CreatePerceptionRequest(
    string Serie, // P001
    DateOnly? IssueDate,
    string CustomerDocType,
    string CustomerDocNumber,
    string CustomerName,
    string? CustomerAddress,
    string RegimeCode, // "01"=Venta interna 2%, "02"=Combustible 1%, "03"=CdP 0.5%
    decimal PerceptionPercent,
    string Currency,
    string? Notes,
    List<CreatePerceptionReferenceRequest> References
);

public record CreatePerceptionReferenceRequest(
    string DocumentType,
    string DocumentNumber,
    DateOnly DocumentDate,
    decimal InvoiceAmount,
    string InvoiceCurrency,
    DateOnly CollectionDate,
    int CollectionNumber,
    decimal CollectionAmount,
    decimal? ExchangeRate,
    DateOnly? ExchangeRateDate
);

public record PerceptionResponse(
    Guid Id,
    string Serie,
    long Correlative,
    string FullNumber,
    DateOnly IssueDate,
    string CustomerDocType,
    string CustomerDocNumber,
    string CustomerName,
    string RegimeCode,
    decimal PerceptionPercent,
    decimal TotalInvoiceAmount,
    decimal TotalPerceived,
    decimal TotalCollected,
    string Currency,
    string Status,
    string? SunatResponseCode,
    string? SunatResponseDescription,
    string? XmlUrl,
    string? PdfUrl,
    DateTimeOffset CreatedAt,
    List<PerceptionReferenceResponse> References
);

public record PerceptionReferenceResponse(
    Guid Id,
    string DocumentType,
    string DocumentNumber,
    DateOnly DocumentDate,
    decimal InvoiceAmount,
    DateOnly CollectionDate,
    decimal CollectionAmount,
    decimal PerceivedAmount,
    decimal TotalCollectedAmount
);
