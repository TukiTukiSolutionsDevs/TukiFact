using TukiFact.Common.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace TukiFact.Common.Presentation.Results;

/// <summary>
/// The only translation from <see cref="Result"/> to HTTP: success → 200/204, failure → RFC 9457
/// ProblemDetails with 400 validation (plus <c>errors</c>), 401 unauthorized, 404 not found, 409
/// conflict, 500 failure and the error code.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? TypedResults.NoContent() : ErrorProblem.From(result.Error);
    }

    public static IResult ToHttpResult<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : ErrorProblem.From(result.Error);
    }
}
