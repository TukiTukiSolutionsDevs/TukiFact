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

        var resetTime = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString();

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "3600";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = "Has excedido el limite de solicitudes por hora de tu plan. Intenta mas tarde o actualiza tu plan.",
                limit,
                reset = resetTime
            });
            return;
        }

        // Monthly document limit check (only for document creation)
        if (context.Request.Method == "POST" && path.Contains("/documents") && !path.Contains("/void"))
        {
            var (monthAllowed, used, monthLimit) = await rateLimiter.CheckMonthlyDocumentsAsync(tenantId, context.RequestAborted);
            context.Response.Headers["X-Monthly-Limit"] = monthLimit.ToString();
            context.Response.Headers["X-Monthly-Used"] = used.ToString();
            context.Response.Headers["X-Monthly-Remaining"] = Math.Max(0, monthLimit - used).ToString();

            if (!monthAllowed)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Monthly document limit exceeded",
                    message = "Has alcanzado el limite mensual de documentos de tu plan. Actualiza tu plan para emitir mas comprobantes.",
                    used,
                    limit = monthLimit
                });
                return;
            }
        }

        await _next(context);
    }
}
