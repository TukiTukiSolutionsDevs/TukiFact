namespace TukiFact.Application.DTOs.Documents;

public record DocumentResponse(
    Guid Id,
    string DocumentType,
    string DocumentTypeName,
    string Serie,
    long Correlative,
    string FullNumber,
    DateOnly IssueDate,
    DateOnly? DueDate,
    string Currency,
    string CustomerDocType,
    string CustomerDocNumber,
    string CustomerName,
    decimal OperacionGravada,
    decimal OperacionExonerada,
    decimal OperacionInafecta,
    decimal Igv,
    decimal Total,
    string Status,
    string? SunatResponseCode,
    string? SunatResponseDescription,
    string? HashCode,
    string? XmlUrl,
    string? PdfUrl,
    string? Notes,
    DateTimeOffset CreatedAt,
    List<DocumentItemResponse> Items
);

public record DocumentItemResponse(
    int Sequence,
    string? ProductCode,
    string Description,
    decimal Quantity,
    string UnitMeasure,
    decimal UnitPrice,
    decimal UnitPriceWithIgv,
    string IgvType,
    decimal IgvAmount,
    decimal Subtotal,
    decimal Total
);
