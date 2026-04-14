using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IQuotationRepository
{
    Task<Quotation?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Quotation> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? status = null, DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default);
    Task<long> GetNextCorrelativeAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Quotation entity, CancellationToken ct = default);
    Task UpdateAsync(Quotation entity, CancellationToken ct = default);
}
