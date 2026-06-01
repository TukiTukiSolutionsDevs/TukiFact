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
    {
        var now = DateTimeOffset.UtcNow;
        return await _context.RecurringInvoices
            .Where(r => r.Status == "active"
                && r.NextEmissionDate.HasValue
                && r.NextEmissionDate.Value <= today
                && (!r.EndDate.HasValue || r.EndDate.Value >= today)
                // Skip rows another worker has locked.
                && (!r.ProcessingLockUntil.HasValue || r.ProcessingLockUntil.Value < now))
            .Include(r => r.Tenant)
            .ToListAsync(ct);
    }

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

    public async Task<bool> TryClaimAsync(Guid id, DateTimeOffset lockUntil, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        // Conditional UPDATE — only one worker wins the race.
        var rowsAffected = await _context.RecurringInvoices
            .Where(r => r.Id == id
                && (!r.ProcessingLockUntil.HasValue || r.ProcessingLockUntil.Value < now))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ProcessingLockUntil, lockUntil), ct);
        return rowsAffected > 0;
    }

    public async Task ReleaseClaimAsync(Guid id, CancellationToken ct = default)
    {
        await _context.RecurringInvoices
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ProcessingLockUntil, (DateTimeOffset?)null), ct);
    }

    public async Task<RecurringInvoiceEmission?> ReserveEmissionAsync(
        RecurringInvoice recurring,
        DateOnly targetDate,
        DateOnly? nextDateAfter,
        CancellationToken ct = default)
    {
        // Single transaction so the emission insert + the parent's NextEmissionDate advance
        // either both succeed or both fail — no torn state if the host crashes after this point.
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var emission = new RecurringInvoiceEmission
            {
                RecurringInvoiceId = recurring.Id,
                TargetDate = targetDate,
                Status = "pending",
            };
            await _context.RecurringInvoiceEmissions.AddAsync(emission, ct);

            recurring.LastEmittedDate = targetDate;
            recurring.NextEmissionDate = nextDateAfter;
            recurring.UpdatedAt = DateTimeOffset.UtcNow;
            _context.RecurringInvoices.Update(recurring);

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return emission;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Another worker (or a previous crash recovery) already reserved this (schedule, date) pair.
            await tx.RollbackAsync(ct);
            return null;
        }
    }

    public async Task FinalizeEmissionAsync(
        RecurringInvoiceEmission emission,
        RecurringInvoice recurring,
        CancellationToken ct = default)
    {
        emission.CompletedAt = DateTimeOffset.UtcNow;
        _context.RecurringInvoiceEmissions.Update(emission);

        recurring.UpdatedAt = DateTimeOffset.UtcNow;
        _context.RecurringInvoices.Update(recurring);

        await _context.SaveChangesAsync(ct);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        // Npgsql wraps the PG error inside InnerException.
        var inner = ex.InnerException?.GetType().Name ?? string.Empty;
        var message = ex.InnerException?.Message ?? ex.Message;
        return inner == "PostgresException" && message.Contains("23505"); // unique_violation
    }
}
