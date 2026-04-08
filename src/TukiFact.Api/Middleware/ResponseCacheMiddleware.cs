namespace TukiFact.Api.Middleware;

/// <summary>
/// Simple response cache headers for GET endpoints.
/// Plans, health, and static data can be cached.
/// </summary>
public class ResponseCacheMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Dictionary<string, int> CacheableRoutes = new()
    {
        { "/v1/plans", 300 },        // 5 min
        { "/api/ping", 60 },          // 1 min
        { "/health", 10 },             // 10 sec
    };

    public ResponseCacheMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == "GET")
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            foreach (var (route, seconds) in CacheableRoutes)
            {
                if (path.StartsWith(route))
                {
                    context.Response.Headers.CacheControl = $"public, max-age={seconds}";
                    break;
                }
            }
        }

        await _next(context);
    }
}
