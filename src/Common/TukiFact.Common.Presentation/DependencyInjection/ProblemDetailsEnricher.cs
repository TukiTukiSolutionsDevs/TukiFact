using System.Diagnostics;
using TukiFact.Common.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TukiFact.Common.Presentation.DependencyInjection;

/// <summary>
/// Runs for every ProblemDetails the API writes (Result failures, exception handlers, status code
/// pages) after the framework defaults (<c>type</c>, <c>title</c>): adds <c>traceId</c> (W3C trace
/// id, searchable in Tempo) and <c>correlationId</c>. Exception text is added only in Development.
/// </summary>
internal static class ProblemDetailsEnricher
{
    public const string TraceIdExtension = "traceId";
    public const string CorrelationIdExtension = "correlationId";

    public static void Enrich(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        var problem = context.ProblemDetails;

        problem.Extensions[TraceIdExtension] = Activity.Current?.TraceId.ToHexString() ?? httpContext.TraceIdentifier;

        if (httpContext.RequestServices.GetService<ICorrelationIdAccessor>() is { } correlation)
        {
            problem.Extensions[CorrelationIdExtension] = correlation.CorrelationId;
        }

        if (context.Exception is { } exception
            && problem.Detail is null
            && httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            problem.Detail = exception.ToString();
        }
    }
}
