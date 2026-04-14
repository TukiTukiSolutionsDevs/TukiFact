using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification, CancellationToken ct = default);
    Task<List<Notification>> GetByTenantAsync(Guid tenantId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid tenantId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid tenantId, CancellationToken ct = default);
}
