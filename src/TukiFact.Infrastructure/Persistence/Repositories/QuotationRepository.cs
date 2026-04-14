using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class QuotationRepository : IQuotationRepository
{
    private readonly AppDbContext _context;

    public QuotationRepository(AppDbContext context) => _context = context;

    public async Task<Quotation?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        => await _context.Quotations
            .Include(q => q.Items.OrderBy(i => i.Sequence))
            .Include(q => q.Tenant)
            .FirstOrDefaultAsync(q => q.Id == id, ct);

    public async Task<(IReadOnlyList<Quotation> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? status = null, DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var query = _context.Quotations.Where(q => q.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(q => q.Status == status);
        if (dateFrom.HasValue)
            query = query.Where(q => q.IssueDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(q => q.IssueDate <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(q => q.Items.OrderBy(i => i.Sequence))
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<long> GetNextCorrelativeAsync(Guid tenantId, CancellationToken ct = default)
    {
        var max = await _context.Quotations
            .Where(q => q.TenantId == tenantId)
            .MaxAsync(q => (long?)q.Correlative, ct);
        return (max ?? 0) + 1;
    }

    public async Task AddAsync(Quotation entity, CancellationToken ct = default)
    {
        await _context.Quotations.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Quotation entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        _context.Quotations.Update(entity);
        await _context.SaveChangesAsync(ct);
    }
}
