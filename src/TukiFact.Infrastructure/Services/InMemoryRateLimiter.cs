using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// In-memory rate limiter with per-plan hourly limits.
/// Uses sliding window per tenant (not per endpoint) for simplicity.
/// </summary>
public class InMemoryRateLimiter : IRateLimiter
{
    private readonly IPlanRepository _planRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly AppDbContext _db;
    private readonly ILogger<InMemoryRateLimiter> _logger;
    private static readonly ConcurrentDictionary<string, RateLimitWindow> _windows = new();

    public InMemoryRateLimiter(IPlanRepository planRepo, ITenantRepository tenantRepo, AppDbContext db, ILogger<InMemoryRateLimiter> logger)
    {
        _planRepo = planRepo;
        _tenantRepo = tenantRepo;
        _db = db;
        _logger = logger;
    }

    public async Task<(bool Allowed, int Remaining, int Limit)> CheckAsync(Guid tenantId, string endpoint, CancellationToken ct = default)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        var plan = tenant?.PlanId is not null ? await _planRepo.GetByIdAsync(tenant.PlanId.Value, ct) : null;

        // Hourly request limits per plan (M1.5 spec)
        var limit = plan?.Name switch
        {
            "Free" => 100,
            "Emprendedor" => 500,
            "Negocio" => 2_000,
            "Developer" => 2_000,
            "Profesional" => 5_000,
            "Empresa" => 20_000,
            _ => 100 // Default to Free tier
        };

        var key = $"rate:{tenantId}";
        var now = DateTimeOffset.UtcNow;
        var window = _windows.GetOrAdd(key, _ => new RateLimitWindow { Start = now, Count = 0 });

        // Reset window every hour
        if (now - window.Start > TimeSpan.FromHours(1))
        {
            window.Start = now;
            window.Count = 0;
        }

        window.Count++;
        var remaining = Math.Max(0, limit - window.Count);
        var allowed = window.Count <= limit;

        if (!allowed)
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId} (plan: {Plan}): {Count}/{Limit} requests/hour",
                tenantId, plan?.Name ?? "unknown", window.Count, limit);

        return (allowed, remaining, limit);
    }

    public async Task<(bool Allowed, int Used, int Limit)> CheckMonthlyDocumentsAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Check active subscription first
        var subscription = await _db.Subscriptions
            .Where(s => s.TenantId == tenantId && s.Status == "active")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (subscription is not null)
        {
            var allowed = subscription.DocumentsUsedThisMonth < subscription.DocumentsLimit;
            return (allowed, subscription.DocumentsUsedThisMonth, subscription.DocumentsLimit);
        }

        // Fallback: check plan limits directly
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        var plan = tenant?.PlanId is not null ? await _planRepo.GetByIdAsync(tenant.PlanId.Value, ct) : null;

        var limit = plan?.MaxDocumentsPerMonth ?? 50; // Default free tier
        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var used = await _db.Documents
            .CountAsync(d => d.TenantId == tenantId && d.CreatedAt >= monthStart, ct);

        return (used < limit, used, limit);
    }

    private class RateLimitWindow
    {
        public DateTimeOffset Start { get; set; }
        public int Count { get; set; }
    }
}
