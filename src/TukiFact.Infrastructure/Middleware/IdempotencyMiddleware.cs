using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Middleware;

/// <summary>
/// Honors the `Idempotency-Key` request header on emit endpoints.
/// Pattern: (tenant, key) is the unique slot; body must match on replay.
///
///   - Header missing → pass through (no idempotency).
///   - Header present, no row stored → execute, capture 2xx response, persist for 24h.
///   - Header present, row exists, same body hash → replay stored response (header `X-Idempotent-Replay: true`).
///   - Header present, row exists, different body hash → 409 Conflict (key reused with different payload).
///
/// Registered in Program.cs after authentication + before MapControllers so the
/// tenant id is already resolved from the JWT claim.
/// </summary>
public class IdempotencyMiddleware
{
    private static readonly string[] RelevantPathPrefixes = new[]
    {
        "/v1/documents",
        "/v1/perceptions",
        "/v1/retentions",
        "/v1/voided-documents",
        "/v1/despatch-advices",
        "/v1/recurring-invoices",
    };

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        AppDbContext db,
        ILogger<IdempotencyMiddleware> logger)
    {
        if (!IsRelevant(context))
        {
            await _next(context);
            return;
        }

        var rawKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            await _next(context);
            return;
        }

        if (rawKey.Length > 100 || !IsKeySafe(rawKey))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Idempotency-Key inválido. Máx 100 caracteres alfanuméricos / -_."
            });
            return;
        }

        // Read body to compute hash, re-allow controller to read it.
        context.Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }
        context.Request.Body.Position = 0;

        var bodyHash = Sha256Hex(body);
        var tenantId = ResolveTenantId(context);
        var fullKey = $"{context.Request.Path.ToString().ToLowerInvariant()}:{rawKey}";
        var now = DateTimeOffset.UtcNow;

        var existing = await db.IdempotencyKeys
            .Where(x => x.TenantId == tenantId && x.Key == fullKey && x.ExpiresAt > now)
            .FirstOrDefaultAsync(context.RequestAborted);

        if (existing is not null)
        {
            if (existing.RequestHash != bodyHash)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Idempotency-Key reusada con un cuerpo distinto al original."
                });
                return;
            }

            context.Response.StatusCode = existing.ResponseStatus;
            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Idempotent-Replay"] = "true";
            await context.Response.WriteAsync(existing.ResponseBody);
            return;
        }

        // Buffer the response so we can persist it AFTER the controller runs.
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            buffer.Position = 0;
            var capturedBody = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                try
                {
                    db.IdempotencyKeys.Add(new IdempotencyKey
                    {
                        TenantId = tenantId,
                        Key = fullKey,
                        RequestHash = bodyHash,
                        Endpoint = context.Request.Path.ToString(),
                        ResponseStatus = context.Response.StatusCode,
                        ResponseBody = capturedBody,
                        CreatedAt = now,
                        ExpiresAt = now.AddHours(24),
                    });
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    // Unique-violation = another request just landed with same key — fine, ours already returned.
                    logger.LogDebug(ex, "Idempotency-Key {Key} race lost on persist, ignoring", fullKey);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not persist Idempotency-Key {Key}", fullKey);
                }
            }
        }
    }

    private static bool IsRelevant(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method)) return false;
        var path = context.Request.Path.ToString();
        return RelevantPathPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsKeySafe(string key)
    {
        foreach (var c in key)
        {
            if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_')) return false;
        }
        return true;
    }

    private static Guid? ResolveTenantId(HttpContext context)
    {
        var claim = context.User?.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
        => app.UseMiddleware<IdempotencyMiddleware>();
}
