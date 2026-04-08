namespace TukiFact.Application.DTOs.Documents;

public record VoidDocumentRequest(
    Guid DocumentId,
    string VoidReason
);

public record VoidedDocumentResponse(
    Guid Id,
    string TicketNumber,
    string Status,
    string? SunatTicket,
    string? SunatResponseCode,
    string? SunatResponseDescription,
    DateTimeOffset CreatedAt
);
