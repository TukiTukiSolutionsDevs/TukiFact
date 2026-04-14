using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db) => _db = db;

    public async Task<Notification> CreateAsync(Notification notification, CancellationToken ct = default)
    {
        await _db.Notifications.AddAsync(notification, ct);
        await _db.SaveChangesAsync(ct);
        return notification;
    }

    public async Task<List<Notification>> GetByTenantAsync(Guid tenantId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        return await _db.Notifications
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _db.Notifications
            .CountAsync(n => n.TenantId == tenantId && !n.IsRead, ct);
    }

    public async Task MarkAsReadAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.Id == id && n.TenantId == tenantId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    public async Task MarkAllAsReadAsync(Guid tenantId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.TenantId == tenantId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
