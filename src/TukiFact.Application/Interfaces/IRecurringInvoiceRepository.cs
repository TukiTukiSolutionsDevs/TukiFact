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
}
