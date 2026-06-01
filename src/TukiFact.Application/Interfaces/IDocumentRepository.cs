using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Document?> GetByIdWithItemsAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<(IReadOnlyList<Document> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId, int page, int pageSize,
        string? documentType = null, string? status = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default);
    Task<Document> CreateAsync(Document document, CancellationToken ct = default);
    Task UpdateAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Count of documents this tenant has emitted in the current Lima-time calendar
    /// month. Used to gate plan-limit enforcement. Excludes documents that never
    /// reached SUNAT (Status = Draft / Rejected pre-send).
    /// </summary>
    Task<int> CountForCurrentMonthAsync(Guid tenantId, CancellationToken ct = default);
}
