using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class RecurringInvoiceRepository : IRecurringInvoiceRepository
{
    private readonly AppDbContext _context;

    public RecurringInvoiceRepository(AppDbContext context) => _context = context;

    public async Task<RecurringInvoice?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.RecurringInvoices
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<(IReadOnlyList<RecurringInvoice> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? status = null, CancellationToken ct = default)
    {
        var query = _context.RecurringInvoices.Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<RecurringInvoice>> GetDueForEmissionAsync(DateOnly today, CancellationToken ct = default)
        => await _context.RecurringInvoices
            .Where(r => r.Status == "active"
                && r.NextEmissionDate.HasValue
                && r.NextEmissionDate.Value <= today
                && (!r.EndDate.HasValue || r.EndDate.Value >= today))
            .Include(r => r.Tenant)
            .ToListAsync(ct);

    public async Task AddAsync(RecurringInvoice entity, CancellationToken ct = default)
    {
        await _context.RecurringInvoices.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RecurringInvoice entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        _context.RecurringInvoices.Update(entity);
        await _context.SaveChangesAsync(ct);
    }
}
