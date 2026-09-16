using Scalar.AspNetCore;

namespace TukiFact.Api.OpenApi;

public static class ApiDocumentationEndpointRouteBuilderExtensions
{
    public const string ReferenceRoute = "/scalar";

    /// <summary>
    /// Development only: the anonymous OpenAPI document and the Scalar reference UI. Production
    /// maps neither; clients use the committed <c>openapi/tukifact-api-v1.json</c>. Call after
    /// <c>MapEndpoints</c>/<c>MapControllers</c> so every route is already in the route table.
    /// </summary>
    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(ReferenceRoute, options =>
            {
                options.WithTitle("TukiFact API");
                options.AddDocument(ApiDocumentationServiceCollectionExtensions.DocumentName);
            })
            .AllowAnonymous();

        return app;
    }
}
