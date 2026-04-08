using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface ISeriesRepository
{
    Task<Series?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Series>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<Series?> GetByTypeAndSerieAsync(Guid tenantId, string documentType, string serie, CancellationToken ct = default);
    Task<Series> CreateAsync(Series series, CancellationToken ct = default);
    Task UpdateAsync(Series series, CancellationToken ct = default);
    Task<long> GetNextCorrelativeAsync(Guid seriesId, CancellationToken ct = default);
}
