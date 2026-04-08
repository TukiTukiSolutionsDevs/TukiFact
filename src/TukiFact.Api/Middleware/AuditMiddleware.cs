using System.Security.Claims;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] AuditedMethods = ["POST", "PUT", "DELETE", "PATCH"];
    private static readonly string[] ExcludedPaths = ["/health", "/openapi", "/api/ping"];

    public AuditMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Only audit write operations that succeeded
        if (!AuditedMethods.Contains(context.Request.Method)) return;
        if (context.Response.StatusCode >= 400) return;

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (ExcludedPaths.Any(p => path.StartsWith(p))) return;

        var tenantProvider = context.RequestServices.GetService<ITenantProvider>();
        var tenantId = tenantProvider?.GetCurrentTenantId() ?? Guid.Empty;
        if (tenantId == Guid.Empty) return;

        try
        {
            var auditRepo = context.RequestServices.GetRequiredService<IAuditLogRepository>();
            var userId = context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            var action = MapPathToAction(context.Request.Method, path);
            var entityType = MapPathToEntityType(path);

            await auditRepo.LogAsync(new AuditLog
            {
                TenantId = tenantId,
                UserId = userId is not null ? Guid.Parse(userId) : null,
                Action = action,
                EntityType = entityType,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                Details = $"{{\"method\":\"{context.Request.Method}\",\"path\":\"{path}\",\"status\":{context.Response.StatusCode}}}"
            });
        }
        catch { /* Audit logging should never break the request */ }
    }

    private static string MapPathToAction(string method, string path) => (method, path) switch
    {
        ("POST", var p) when p.Contains("/auth/register") => "tenant.registered",
        ("POST", var p) when p.Contains("/auth/login") => "user.login",
        ("POST", var p) when p.Contains("/documents/credit-note") => "creditnote.created",
        ("POST", var p) when p.Contains("/documents/debit-note") => "debitnote.created",
        ("POST", var p) when p.Contains("/documents") => "document.created",
        ("POST", var p) when p.Contains("/voided") => "document.voided",
        ("POST", var p) when p.Contains("/users") => "user.created",
        ("POST", var p) when p.Contains("/api-keys") => "apikey.generated",
        ("POST", var p) when p.Contains("/series") => "series.created",
        ("POST", var p) when p.Contains("/webhooks") => "webhook.created",
        ("PUT", _) => $"{method.ToLower()}.updated",
        ("DELETE", _) => $"{method.ToLower()}.deleted",
        _ => $"{method.ToLower()}.action"
    };

    private static string MapPathToEntityType(string path) => path switch
    {
        var p when p.Contains("/documents") => "Document",
        var p when p.Contains("/users") => "User",
        var p when p.Contains("/api-keys") => "ApiKey",
        var p when p.Contains("/series") => "Series",
        var p when p.Contains("/webhooks") => "Webhook",
        var p when p.Contains("/voided") => "VoidedDocument",
        var p when p.Contains("/auth") => "Auth",
        _ => "Unknown"
    };
}
