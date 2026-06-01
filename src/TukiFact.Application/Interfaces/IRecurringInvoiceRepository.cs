using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IRecurringInvoiceRepository
{
    Task<RecurringInvoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<RecurringInvoice> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? status = null, CancellationToken ct = default);
    Task<IReadOnlyList<RecurringInvoice>> GetDueForEmissionAsync(DateOnly today, CancellationToken ct = default);
    Task AddAsync(RecurringInvoice entity, CancellationToken ct = default);
    Task UpdateAsync(RecurringInvoice entity, CancellationToken ct = default);

    /// <summary>
    /// Atomic claim — sets ProcessingLockUntil only if no live lock exists. Returns true if
    /// this worker now owns the row. Prevents two scheduler instances from emitting the same row
    /// during deploy overlap.
    /// </summary>
    Task<bool> TryClaimAsync(Guid id, DateTimeOffset lockUntil, CancellationToken ct = default);

    /// <summary>Release the lease — called in finally blocks so a crash unlocks via timeout, not orphaned forever.</summary>
    Task ReleaseClaimAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Reserve an emission slot — atomically inserts a pending RecurringInvoiceEmission and
    /// advances the parent's NextEmissionDate / LastEmittedDate in the same transaction.
    /// Returns the inserted emission row, or null if a row already existed for that (schedule, date)
    /// pair (idempotent retry collision).
    /// </summary>
    Task<RecurringInvoiceEmission?> ReserveEmissionAsync(
        RecurringInvoice recurring,
        DateOnly targetDate,
        DateOnly? nextDateAfter,
        CancellationToken ct = default);

    /// <summary>Update emission terminal state (succeeded / rejected / error) plus parent failure counters.</summary>
    Task FinalizeEmissionAsync(
        RecurringInvoiceEmission emission,
        RecurringInvoice recurring,
        CancellationToken ct = default);
}
