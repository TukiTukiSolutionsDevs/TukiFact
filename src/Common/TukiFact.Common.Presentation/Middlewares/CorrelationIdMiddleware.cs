using System.Diagnostics;
using TukiFact.Common.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TukiFact.Common.Presentation.Middlewares;

/// <summary>
/// Propagates <c>X-Correlation-Id</c>: a well-formed incoming id is kept, otherwise the scope's
/// <see cref="ICorrelationIdAccessor"/> default (the trace id) is used. The id is echoed in the
/// response, set as log scope and activity tag. Not added as W3C baggage: baggage would leak it to
/// every third-party HTTP call.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string ActivityTag = "tukifact.correlation_id";
    private const string LogScopeKey = "CorrelationId";
    private const int MaxLength = 128;

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlation)
    {
        var incoming = context.Request.Headers[ICorrelationIdAccessor.HeaderName];
        if (incoming.Count == 1 && IsWellFormed(incoming[0]))
        {
            correlation.Set(incoming[0]!);
        }

        var correlationId = correlation.CorrelationId;

        // OnStarting survives the exception handler clearing the response, so error responses carry the header too.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ICorrelationIdAccessor.HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag(ActivityTag, correlationId);

        using (logger.BeginScope(new Dictionary<string, object> { [LogScopeKey] = correlationId }))
        {
            await next(context);
        }
    }

    internal static bool IsWellFormed(string? value) =>
        !string.IsNullOrEmpty(value) && value.Length <= MaxLength && value.All(IsAllowed);

    private static bool IsAllowed(char character) =>
        char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':';
}
