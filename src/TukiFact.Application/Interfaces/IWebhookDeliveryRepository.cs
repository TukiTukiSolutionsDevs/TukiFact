using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IWebhookDeliveryRepository
{
    Task<IReadOnlyList<WebhookDelivery>> GetByWebhookAsync(Guid webhookConfigId, int limit = 20, CancellationToken ct = default);
    Task<WebhookDelivery> CreateAsync(WebhookDelivery delivery, CancellationToken ct = default);
    Task UpdateAsync(WebhookDelivery delivery, CancellationToken ct = default);
}
