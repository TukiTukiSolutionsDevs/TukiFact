using Microsoft.EntityFrameworkCore;
using Npgsql;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class DespatchAdviceRepository : IDespatchAdviceRepository
{
    private const string UniqueViolationSqlState = "23505";
    private const int MaxCorrelativeRetries = 8;

    private readonly AppDbContext _context;

    public DespatchAdviceRepository(AppDbContext context) => _context = context;

    public async Task<DespatchAdvice?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        => await _context.DespatchAdvices
            .Include(d => d.Items.OrderBy(i => i.LineNumber))
            .Include(d => d.Tenant)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<(IReadOnlyList<DespatchAdvice> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? documentType = null, string? status = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var query = _context.DespatchAdvices
            .Where(d => d.TenantId == tenantId);

        if (!string.IsNullOrEmpty(documentType))
            query = query.Where(d => d.DocumentType == documentType);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);
        if (dateFrom.HasValue)
            query = query.Where(d => d.IssueDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(d => d.IssueDate <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(d => d.Items.OrderBy(i => i.LineNumber))
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<long> GetNextCorrelativeAsync(Guid tenantId, string serie, CancellationToken ct = default)
    {
        var maxCorrelative = await _context.DespatchAdvices
            .Where(d => d.TenantId == tenantId && d.Serie == serie)
            .MaxAsync(d => (long?)d.Correlative, ct);

        return (maxCorrelative ?? 0) + 1;
    }

    /// <summary>
    /// Adds a DespatchAdvice with an atomically-assigned correlative. The (TenantId, Serie, Correlative)
    /// unique index in the DB acts as the source of truth; if two concurrent inserts collide on the same
    /// correlative, we recompute MAX+1 and retry. Bounded to avoid runaway retries.
    /// </summary>
    public async Task AddAsync(DespatchAdvice entity, CancellationToken ct = default)
    {
        // The service may already have called GetNextCorrelativeAsync — honour that as the first attempt.
        if (entity.Correlative <= 0)
        {
            entity.Correlative = await GetNextCorrelativeAsync(entity.TenantId, entity.Serie, ct);
        }

        _context.DespatchAdvices.Add(entity);

        for (var attempt = 0; attempt < MaxCorrelativeRetries; attempt++)
        {
            try
            {
                await _context.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException ex) when (IsCorrelativeCollision(ex))
            {
                // Detach and re-attempt with a fresh correlative.
                _context.Entry(entity).State = EntityState.Detached;
                entity.Correlative = await GetNextCorrelativeAsync(entity.TenantId, entity.Serie, ct);
                _context.DespatchAdvices.Add(entity);
            }
        }

        throw new InvalidOperationException(
            $"No se pudo asignar correlativo único para serie {entity.Serie} tras {MaxCorrelativeRetries} intentos. " +
            "Reintenta en unos segundos.");
    }

    public async Task UpdateAsync(DespatchAdvice entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        _context.DespatchAdvices.Update(entity);
        await _context.SaveChangesAsync(ct);
    }

    private static bool IsCorrelativeCollision(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg && pg.SqlState == UniqueViolationSqlState;
    }
}
