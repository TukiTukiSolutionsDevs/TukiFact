using Microsoft.Extensions.DependencyInjection;

namespace TukiFact.Api.OpenApi;

public static class ApiDocumentationServiceCollectionExtensions
{
    /// <summary>The single OpenAPI document name: matches the committed <c>openapi/tukifact-api-v1.json</c>.</summary>
    public const string DocumentName = "v1";

    /// <summary>
    /// One unversioned OpenAPI 3.1 document (<see cref="DocumentName"/>) covering both the 33
    /// legacy MVC controllers and any future kernel endpoint, shaped by the ported transformers.
    /// <c>Asp.Versioning.OpenApi</c>/<c>AddApiExplorer()</c> are deliberately not used — the
    /// legacy controllers carry no <c>[ApiVersion]</c>, and <c>AssumeDefaultVersionWhenUnspecified=false</c>
    /// would drop them from every version group.
    /// </summary>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenApi(DocumentName, options => options
            .AddDocumentTransformer<ApiInfoDocumentTransformer>()
            .AddOperationTransformer<OperationIdTransformer>()
            .AddOperationTransformer<SecurityRequirementTransformer>()
            .AddSchemaTransformer<ProblemDetailsSchemaTransformer>());

        return services;
    }
}
