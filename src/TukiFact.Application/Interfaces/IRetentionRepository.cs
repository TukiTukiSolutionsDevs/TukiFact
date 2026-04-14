using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IRetentionRepository
{
    Task<RetentionDocument?> GetByIdWithReferencesAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<RetentionDocument> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? status = null, DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default);
    Task<long> GetNextCorrelativeAsync(Guid tenantId, string serie, CancellationToken ct = default);
    Task AddAsync(RetentionDocument entity, CancellationToken ct = default);
    Task UpdateAsync(RetentionDocument entity, CancellationToken ct = default);
}
