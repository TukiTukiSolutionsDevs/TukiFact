using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Logging;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Behaviors;

/// <summary>
/// Logs the use-case outcome with the scope's correlation id (<see cref="ICorrelationIdAccessor"/>, falling back to
/// the current trace id when no accessor is registered). Request payloads are never logged, so passwords,
/// tokens and card data cannot leak.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICorrelationIdAccessor? correlation = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var useCase = UseCaseDiagnostics.UseCaseOf(typeof(TRequest));
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlation?.CorrelationId ?? Activity.Current?.TraceId.ToHexString(),
            ["UseCase"] = useCase,
        });

        logger.UseCaseHandling(useCase);
        var startedAt = Stopwatch.GetTimestamp();

        TResponse response;
        try
        {
            response = await next();
        }
        catch (Exception exception)
        {
            logger.UseCaseCrashed(useCase, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, exception);
            throw;
        }

        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        if (response.IsSuccess)
        {
            logger.UseCaseSucceeded(useCase, elapsedMilliseconds);
        }
        else
        {
            logger.UseCaseFailed(useCase, response.Error.Code, response.Error.Type, elapsedMilliseconds);
        }

        return response;
    }
}
