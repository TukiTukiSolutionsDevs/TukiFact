using TukiFact.Application.DTOs.Documents;

namespace TukiFact.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponse> EmitAsync(CreateDocumentRequest request, Guid tenantId, CancellationToken ct = default);
    Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<DocumentResponse> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? documentType = null, string? status = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default);
    Task<DocumentResponse> EmitCreditNoteAsync(CreateCreditNoteRequest request, Guid tenantId, CancellationToken ct = default);
    Task<DocumentResponse> EmitDebitNoteAsync(CreateDebitNoteRequest request, Guid tenantId, CancellationToken ct = default);
}
