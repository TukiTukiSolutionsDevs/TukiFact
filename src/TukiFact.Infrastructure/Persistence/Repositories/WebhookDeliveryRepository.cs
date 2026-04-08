using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class WebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly AppDbContext _context;
    public WebhookDeliveryRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<WebhookDelivery>> GetByWebhookAsync(Guid webhookConfigId, int limit = 20, CancellationToken ct = default)
        => await _context.WebhookDeliveries.Where(d => d.WebhookConfigId == webhookConfigId)
            .OrderByDescending(d => d.CreatedAt).Take(limit).ToListAsync(ct);

    public async Task<WebhookDelivery> CreateAsync(WebhookDelivery delivery, CancellationToken ct = default)
    {
        await _context.WebhookDeliveries.AddAsync(delivery, ct);
        await _context.SaveChangesAsync(ct);
        return delivery;
    }

    public async Task UpdateAsync(WebhookDelivery delivery, CancellationToken ct = default)
    {
        _context.WebhookDeliveries.Update(delivery);
        await _context.SaveChangesAsync(ct);
    }
}
