namespace TukiFact.Application.DTOs.Documents;

public record CreateCreditNoteRequest(
    string Serie,                   // F001 or B001
    Guid ReferenceDocumentId,       // Original document to credit
    string CreditNoteReason,        // Catálogo 09 code
    string? Description,            // Optional description
    string Currency,
    List<CreateDocumentItemRequest> Items
);
