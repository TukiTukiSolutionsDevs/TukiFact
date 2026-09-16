using System.Text.Json.Serialization;
using TukiFact.Common.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace TukiFact.Common.Presentation.Results;

/// <summary>ProblemDetails for a failed <see cref="Result"/>; <c>type</c>, <c>title</c> and trace data are added by the ProblemDetails service.</summary>
internal static class ErrorProblem
{
    public const string CodeExtension = "code";
    public const string ErrorsExtension = "errors";

    /// <summary>A failure is a server-side fault: its description may name internals, so only the code leaves the API.</summary>
    private const string FailureDetail = "An unexpected error occurred.";

    public static IResult From(Error error) => From(error, StatusCodeOf(error.Type));

    /// <summary>
    /// The same body with a status the error category does not carry (e.g. 413 for an oversized
    /// webhook). <c>errors</c> travels only with 400.
    /// </summary>
    public static IResult From(Error error, int statusCode)
    {
        var extensions = new Dictionary<string, object?> { [CodeExtension] = error.Code };
        if (error.Type == ErrorType.Validation && statusCode == StatusCodes.Status400BadRequest)
        {
            extensions[ErrorsExtension] = error.FieldErrors
                .Select(fieldError => new FieldErrorBody(fieldError.Field, fieldError.Code, fieldError.Description))
                .ToArray();
        }

        return TypedResults.Problem(
            detail: error.Type == ErrorType.Failure ? FailureDetail : error.Description,
            statusCode: statusCode,
            extensions: extensions);
    }

    private static int StatusCodeOf(ErrorType type) =>
        type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

    /// <summary>Wire shape of a field error; names are fixed so they do not depend on the host naming policy.</summary>
    private sealed record FieldErrorBody(
        [property: JsonPropertyName("field")] string Field,
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("description")] string Description);
}
