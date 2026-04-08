using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

public class InMemoryRateLimiter : IRateLimiter
{
    private readonly IPlanRepository _planRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly ILogger<InMemoryRateLimiter> _logger;
    private static readonly ConcurrentDictionary<string, RateLimitWindow> _windows = new();

    public InMemoryRateLimiter(IPlanRepository planRepo, ITenantRepository tenantRepo, ILogger<InMemoryRateLimiter> logger)
    {
        _planRepo = planRepo;
        _tenantRepo = tenantRepo;
        _logger = logger;
    }

    public async Task<(bool Allowed, int Remaining, int Limit)> CheckAsync(Guid tenantId, string endpoint, CancellationToken ct = default)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        var plan = tenant?.PlanId is not null ? await _planRepo.GetByIdAsync(tenant.PlanId.Value, ct) : null;

        // Default limits per minute based on plan
        var limit = plan?.Name switch
        {
            "Free" => 10,
            "Emprendedor" => 100,
            "Negocio" => 300,
            "Developer" => 500,
            "Profesional" => 500,
            "Empresa" => 1000,
            _ => 30
        };

        var key = $"{tenantId}:{endpoint}";
        var now = DateTimeOffset.UtcNow;
        var window = _windows.GetOrAdd(key, _ => new RateLimitWindow { Start = now, Count = 0 });

        // Reset window every minute
        if (now - window.Start > TimeSpan.FromMinutes(1))
        {
            window.Start = now;
            window.Count = 0;
        }

        window.Count++;
        var remaining = Math.Max(0, limit - window.Count);
        var allowed = window.Count <= limit;

        if (!allowed)
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId} on {Endpoint}: {Count}/{Limit}", tenantId, endpoint, window.Count, limit);

        return (allowed, remaining, limit);
    }

    private class RateLimitWindow
    {
        public DateTimeOffset Start { get; set; }
        public int Count { get; set; }
    }
}
