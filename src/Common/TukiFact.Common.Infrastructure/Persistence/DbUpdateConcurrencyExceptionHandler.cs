using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// A write issued from a stale <c>sync_version</c> (<see cref="Interceptors.SyncVersionInterceptor"/>) → 409 Conflict,
/// so clients reload and retry. Lives in Infrastructure: Presentation never references EF Core. The exception
/// message (entity and SQL details) never reaches the response.
/// </summary>
internal sealed class DbUpdateConcurrencyExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<DbUpdateConcurrencyExceptionHandler> logger) : IExceptionHandler
{
    public const string Code = "Concurrency.Conflict";
    private const string Detail = "The resource was changed by another request. Reload it and try again.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not DbUpdateConcurrencyException concurrency)
        {
            return false;
        }

        logger.ConcurrencyConflict(concurrency.Entries.Count);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status409Conflict,
                Detail = Detail,
                Extensions = { ["code"] = Code },
            },
        });
    }
}
