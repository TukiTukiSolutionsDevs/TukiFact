using Microsoft.Extensions.Logging;

namespace TukiFact.Common.Presentation.Middlewares;

internal static partial class PresentationLogs
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Warning, Message = "Business rule {RuleCode} was broken")]
    public static partial void BusinessRuleBroken(this ILogger logger, string ruleCode);
}
