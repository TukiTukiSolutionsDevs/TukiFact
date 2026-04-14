using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class DespatchAdviceRepository : IDespatchAdviceRepository
{
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

    public async Task AddAsync(DespatchAdvice entity, CancellationToken ct = default)
    {
        await _context.DespatchAdvices.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DespatchAdvice entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        _context.DespatchAdvices.Update(entity);
        await _context.SaveChangesAsync(ct);
    }
}
