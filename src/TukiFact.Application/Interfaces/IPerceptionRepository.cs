using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IPerceptionRepository
{
    Task<PerceptionDocument?> GetByIdWithReferencesAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<PerceptionDocument> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? status = null, DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default);
    Task<long> GetNextCorrelativeAsync(Guid tenantId, string serie, CancellationToken ct = default);
    Task AddAsync(PerceptionDocument entity, CancellationToken ct = default);
    Task UpdateAsync(PerceptionDocument entity, CancellationToken ct = default);
}
