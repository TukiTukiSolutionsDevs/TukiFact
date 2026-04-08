using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId, int page, int pageSize,
        string? action = null, string? entityType = null,
        CancellationToken ct = default);
    Task LogAsync(AuditLog entry, CancellationToken ct = default);
}
