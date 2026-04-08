using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IWebhookRepository
{
    Task<WebhookConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookConfig>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookConfig>> GetActiveByEventAsync(Guid tenantId, string eventType, CancellationToken ct = default);
    Task<WebhookConfig> CreateAsync(WebhookConfig config, CancellationToken ct = default);
    Task UpdateAsync(WebhookConfig config, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
