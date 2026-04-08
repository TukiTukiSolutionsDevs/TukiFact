using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IVoidedDocumentRepository
{
    Task<VoidedDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<VoidedDocument>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<VoidedDocument> CreateAsync(VoidedDocument doc, CancellationToken ct = default);
    Task UpdateAsync(VoidedDocument doc, CancellationToken ct = default);
    Task<int> GetNextTicketNumberAsync(Guid tenantId, string ticketType, DateOnly date, CancellationToken ct = default);
}
