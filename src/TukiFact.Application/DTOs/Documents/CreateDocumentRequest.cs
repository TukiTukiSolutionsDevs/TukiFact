namespace TukiFact.Application.DTOs.Documents;

public record CreateDocumentRequest(
    string DocumentType,
    string Serie,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    string Currency,
    string CustomerDocType,
    string CustomerDocNumber,
    string CustomerName,
    string? CustomerAddress,
    string? CustomerEmail,
    string? Notes,
    string? PurchaseOrder,
    List<CreateDocumentItemRequest> Items
);

public record CreateDocumentItemRequest(
    string? ProductCode,
    string? SunatProductCode,
    string Description,
    decimal Quantity,
    string UnitMeasure,
    decimal UnitPrice,
    string IgvType,
    decimal Discount = 0
);
