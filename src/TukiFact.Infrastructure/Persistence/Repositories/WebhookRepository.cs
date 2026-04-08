using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class WebhookRepository : IWebhookRepository
{
    private readonly AppDbContext _context;
    public WebhookRepository(AppDbContext context) => _context = context;

    public async Task<WebhookConfig?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.WebhookConfigs.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IReadOnlyList<WebhookConfig>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.WebhookConfigs.Where(w => w.TenantId == tenantId).OrderByDescending(w => w.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<WebhookConfig>> GetActiveByEventAsync(Guid tenantId, string eventType, CancellationToken ct = default)
        => await _context.WebhookConfigs
            .Where(w => w.TenantId == tenantId && w.IsActive)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<WebhookConfig>)t.Result
                .Where(w => JsonSerializer.Deserialize<string[]>(w.Events)?.Contains(eventType) == true)
                .ToList(), ct);

    public async Task<WebhookConfig> CreateAsync(WebhookConfig config, CancellationToken ct = default)
    {
        await _context.WebhookConfigs.AddAsync(config, ct);
        await _context.SaveChangesAsync(ct);
        return config;
    }

    public async Task UpdateAsync(WebhookConfig config, CancellationToken ct = default)
    {
        _context.WebhookConfigs.Update(config);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var config = await _context.WebhookConfigs.FindAsync([id], ct);
        if (config is not null) { _context.WebhookConfigs.Remove(config); await _context.SaveChangesAsync(ct); }
    }
}
