namespace TukiFact.Application.DTOs.Documents;

public record CreateDebitNoteRequest(
    string Serie,
    Guid ReferenceDocumentId,
    string DebitNoteReason,         // Catálogo 10 code
    string? Description,
    string Currency,
    List<CreateDocumentItemRequest> Items
);
