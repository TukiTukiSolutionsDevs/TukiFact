using Microsoft.Extensions.Logging;

namespace TukiFact.Common.Presentation.Grpc;

internal static partial class GrpcLogs
{
    [LoggerMessage(EventId = 3100, Level = LogLevel.Warning, Message = "Business rule {RuleCode} was broken")]
    public static partial void BusinessRuleBroken(this ILogger logger, string ruleCode);

    [LoggerMessage(EventId = 3101, Level = LogLevel.Warning, Message = "Optimistic concurrency conflict on a gRPC call")]
    public static partial void ConcurrencyConflict(this ILogger logger);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Error, Message = "Unhandled exception on a gRPC call")]
    public static partial void UnhandledException(this ILogger logger, Exception exception);
}
