using System.Security.Claims;
using TukiFact.Domain.Interfaces;
using TukiFact.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TukiFact.Api.Middleware;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolverMiddleware> _logger;

    private static readonly string[] ExcludedPaths = [
        "/health",
        "/health/ready",
        "/health/live",
        "/openapi",
        "/v1/auth/register",
        "/v1/auth/login",
        "/v1/auth/refresh",
        "/v1/plans",
        "/v1/backoffice",
        "/api/ping",
        "/metrics"
    ];

    public TenantResolverMiddleware(RequestDelegate next, ILogger<TenantResolverMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        if (ExcludedPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        var tenantProvider = context.RequestServices.GetRequiredService<ITenantProvider>();
        Guid tenantId = Guid.Empty;

        // 1. Try JWT claims first (from authenticated user)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            if (tenantClaim is not null && Guid.TryParse(tenantClaim, out var jwtTenantId))
            {
                tenantId = jwtTenantId;
            }
        }

        // 2. Fallback: X-Tenant-Id header (for API key auth or testing)
        if (tenantId == Guid.Empty
            && context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader)
            && Guid.TryParse(tenantIdHeader, out var headerTenantId))
        {
            tenantId = headerTenantId;
        }

        if (tenantId != Guid.Empty)
        {
            tenantProvider.SetCurrentTenantId(tenantId);

            // Set PostgreSQL RLS context
            var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
            #pragma warning disable EF1002 // SET LOCAL doesn't support parameterized queries in PG
            await dbContext.Database.ExecuteSqlRawAsync(
                $"SET LOCAL app.current_tenant = '{tenantId}'");
            #pragma warning restore EF1002

            _logger.LogDebug("Tenant resolved: {TenantId} (source: {Source})",
                tenantId, context.User.Identity?.IsAuthenticated == true ? "JWT" : "Header");
        }

        await _next(context);
    }
}
