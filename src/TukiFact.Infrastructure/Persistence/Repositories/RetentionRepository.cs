using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class RetentionRepository : IRetentionRepository
{
    private readonly AppDbContext _context;

    public RetentionRepository(AppDbContext context) => _context = context;

    public async Task<RetentionDocument?> GetByIdWithReferencesAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _context.RetentionDocuments
            .Include(r => r.References.OrderBy(ref_ => ref_.PaymentDate))
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);

    public async Task<(IReadOnlyList<RetentionDocument> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? status = null, DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var query = _context.RetentionDocuments.Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);
        if (dateFrom.HasValue)
            query = query.Where(r => r.IssueDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(r => r.IssueDate <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(r => r.References)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<long> GetNextCorrelativeAsync(Guid tenantId, string serie, CancellationToken ct = default)
    {
        var max = await _context.RetentionDocuments
            .Where(r => r.TenantId == tenantId && r.Serie == serie)
            .MaxAsync(r => (long?)r.Correlative, ct);
        return (max ?? 0) + 1;
    }

    public async Task<long> AddWithCorrelativeAsync(RetentionDocument entity, string serie, CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({entity.TenantId.ToString() + ":" + serie}))", ct);

            var max = await _context.RetentionDocuments
                .Where(r => r.TenantId == entity.TenantId && r.Serie == serie)
                .MaxAsync(r => (long?)r.Correlative, ct);
            entity.Correlative = (max ?? 0) + 1;

            await _context.RetentionDocuments.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        });
        return entity.Correlative;
    }

    public async Task AddAsync(RetentionDocument entity, CancellationToken ct = default)
    {
        await _context.RetentionDocuments.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RetentionDocument entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        _context.RetentionDocuments.Update(entity);
        await _context.SaveChangesAsync(ct);
    }
}
