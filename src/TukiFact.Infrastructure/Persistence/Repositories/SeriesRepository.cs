using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class SeriesRepository : ISeriesRepository
{
    private readonly AppDbContext _context;

    public SeriesRepository(AppDbContext context) => _context = context;

    public async Task<Series?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Series.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Series>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Series.Where(s => s.TenantId == tenantId && s.IsActive).OrderBy(s => s.DocumentType).ThenBy(s => s.Serie).ToListAsync(ct);

    public async Task<Series?> GetByTypeAndSerieAsync(Guid tenantId, string documentType, string serie, CancellationToken ct = default)
        => await _context.Series.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.DocumentType == documentType && s.Serie == serie, ct);

    public async Task<Series> CreateAsync(Series series, CancellationToken ct = default)
    {
        await _context.Series.AddAsync(series, ct);
        await _context.SaveChangesAsync(ct);
        return series;
    }

    public async Task UpdateAsync(Series series, CancellationToken ct = default)
    {
        _context.Series.Update(series);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<long> GetNextCorrelativeAsync(Guid seriesId, CancellationToken ct = default)
    {
        // Atomic increment + return in a single round-trip to prevent race conditions
        // RETURNING gives us the post-increment value directly
        var rows = await _context.Database.SqlQueryRaw<long>(
            "UPDATE series SET \"CurrentCorrelative\" = \"CurrentCorrelative\" + 1 WHERE \"Id\" = {0} RETURNING \"CurrentCorrelative\"",
            seriesId).ToListAsync(ct);

        return rows.FirstOrDefault();
    }
}
