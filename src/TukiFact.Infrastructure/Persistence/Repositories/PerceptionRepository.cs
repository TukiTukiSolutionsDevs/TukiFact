using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class PerceptionRepository : IPerceptionRepository
{
    private readonly AppDbContext _context;

    public PerceptionRepository(AppDbContext context) => _context = context;

    public async Task<PerceptionDocument?> GetByIdWithReferencesAsync(Guid id, CancellationToken ct = default)
        => await _context.PerceptionDocuments
            .Include(p => p.References.OrderBy(ref_ => ref_.CollectionDate))
            .Include(p => p.Tenant)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<PerceptionDocument> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? status = null, DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var query = _context.PerceptionDocuments.Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        if (dateFrom.HasValue)
            query = query.Where(p => p.IssueDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(p => p.IssueDate <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(p => p.References)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<long> GetNextCorrelativeAsync(Guid tenantId, string serie, CancellationToken ct = default)
    {
        var max = await _context.PerceptionDocuments
            .Where(p => p.TenantId == tenantId && p.Serie == serie)
            .MaxAsync(p => (long?)p.Correlative, ct);
        return (max ?? 0) + 1;
    }

    public async Task AddAsync(PerceptionDocument entity, CancellationToken ct = default)
    {
        await _context.PerceptionDocuments.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PerceptionDocument entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        _context.PerceptionDocuments.Update(entity);
        await _context.SaveChangesAsync(ct);
    }
}
