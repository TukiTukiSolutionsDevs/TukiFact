using TukiFact.Application.Interfaces;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    private static readonly string[] ExcludedPaths = ["/health", "/health/ready", "/health/live", "/openapi", "/api/ping"];

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
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

        var tenantProvider = context.RequestServices.GetService<ITenantProvider>();
        var tenantId = tenantProvider?.GetCurrentTenantId() ?? Guid.Empty;

        if (tenantId == Guid.Empty)
        {
            await _next(context);
            return;
        }

        var rateLimiter = context.RequestServices.GetRequiredService<IRateLimiter>();
        var (allowed, remaining, limit) = await rateLimiter.CheckAsync(tenantId, path);

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Try again later." });
            return;
        }

        await _next(context);
    }
}
