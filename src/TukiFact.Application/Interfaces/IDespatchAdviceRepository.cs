using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IDespatchAdviceRepository
{
    Task<DespatchAdvice?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<DespatchAdvice> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? documentType = null, string? status = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default);
    Task<long> GetNextCorrelativeAsync(Guid tenantId, string serie, CancellationToken ct = default);
    Task AddAsync(DespatchAdvice entity, CancellationToken ct = default);
    Task UpdateAsync(DespatchAdvice entity, CancellationToken ct = default);
}
