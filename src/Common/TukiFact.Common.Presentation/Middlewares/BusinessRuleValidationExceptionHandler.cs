using TukiFact.Common.Domain.Rules;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TukiFact.Common.Presentation.Middlewares;

/// <summary>
/// A broken domain invariant → 409 Conflict: the request is well formed but conflicts with the
/// current state of the aggregate. Format and use-case validation are 400 through <c>Result</c>.
/// The rule message is business language written for the caller, so it is returned as <c>detail</c>.
/// </summary>
internal sealed class BusinessRuleValidationExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<BusinessRuleValidationExceptionHandler> logger) : IExceptionHandler
{
    public const string CodePrefix = "BusinessRule.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not BusinessRuleValidationException brokenRule)
        {
            return false;
        }

        var code = CodePrefix + brokenRule.BrokenRule.GetType().Name;
        logger.BusinessRuleBroken(code);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status409Conflict,
                Detail = brokenRule.BrokenRule.Message,
                Extensions = { ["code"] = code },
            },
        });
    }
}
