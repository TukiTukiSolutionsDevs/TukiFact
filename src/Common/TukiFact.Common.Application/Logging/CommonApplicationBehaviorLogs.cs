using Microsoft.Extensions.Logging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Logging;

internal static partial class CommonApplicationBehaviorLogs
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "Handling use case {UseCase}")]
    public static partial void UseCaseHandling(this ILogger logger, string useCase);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Use case {UseCase} succeeded in {ElapsedMilliseconds} ms")]
    public static partial void UseCaseSucceeded(this ILogger logger, string useCase, double elapsedMilliseconds);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Use case {UseCase} failed with {ErrorCode} ({ErrorType}) in {ElapsedMilliseconds} ms")]
    public static partial void UseCaseFailed(
        this ILogger logger,
        string useCase,
        string errorCode,
        ErrorType errorType,
        double elapsedMilliseconds);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error,
        Message = "Use case {UseCase} crashed after {ElapsedMilliseconds} ms")]
    public static partial void UseCaseCrashed(this ILogger logger, string useCase, double elapsedMilliseconds, Exception exception);
}
