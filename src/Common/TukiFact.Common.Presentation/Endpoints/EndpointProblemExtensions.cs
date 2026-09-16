using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace TukiFact.Common.Presentation.Endpoints;

public static class EndpointProblemExtensions
{
    /// <summary>
    /// Documents the ProblemDetails every use case endpoint can return: 400 validation, 404 not
    /// found, 409 conflict (Result, broken business rule, concurrency) and 500. 401/403 are
    /// documented automatically from the endpoint authorization.
    /// </summary>
    public static TBuilder WithStandardProblems<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
