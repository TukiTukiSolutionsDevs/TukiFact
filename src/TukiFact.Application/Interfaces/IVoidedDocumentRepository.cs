using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IVoidedDocumentRepository
{
    Task<VoidedDocument?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<VoidedDocument>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<VoidedDocument> CreateAsync(VoidedDocument doc, CancellationToken ct = default);
    Task UpdateAsync(VoidedDocument doc, CancellationToken ct = default);
    Task<int> GetNextTicketNumberAsync(Guid tenantId, string ticketType, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Race-safe: advisory_xact_lock(hash(tenant, ticketType, date)) + count+1 + ticket format
    /// + insert in one tx. Caller sets TenantId/TicketType/IssueDate/ReferenceDate/ItemsJson;
    /// this method writes TicketNumber.
    /// </summary>
    Task CreateWithTicketAsync(VoidedDocument entity, CancellationToken ct = default);

    /// <summary>Worker pickup: pending tickets that haven't exhausted retries.</summary>
    Task<IReadOnlyList<VoidedDocument>> GetPendingForWorkerAsync(int maxRetries, int batchSize, CancellationToken ct = default);

    /// <summary>Worker pickup: sent tickets whose getStatus is due (LastPolledAt older than `pollEverySeconds`).</summary>
    Task<IReadOnlyList<VoidedDocument>> GetSentForPollAsync(int pollEverySeconds, int batchSize, CancellationToken ct = default);
}
