using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TukiFact.Api.OpenApi;

/// <summary>Document identity, a relative server (clients choose the host) and the HTTP bearer JWT scheme.</summary>
public sealed class ApiInfoDocumentTransformer : IOpenApiDocumentTransformer
{
    public const string BearerSchemeName = "Bearer";

    private const string Title = "TukiFact API";

    private const string Description =
        "TukiFact HTTP API. Errors are RFC 9457 `application/problem+json` bodies with `traceId`, " +
        "`correlationId` and, for application errors, a stable `code`. Send `X-Correlation-Id` to trace a request.";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        document.Info ??= new OpenApiInfo();
        document.Info.Title = Title;
        document.Info.Version = context.DocumentName;
        document.Info.Description = Description;
        document.Info.Contact = null;

        document.Servers = [new OpenApiServer { Url = "/" }];

        document.AddComponent<IOpenApiSecurityScheme>(BearerSchemeName, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "HS256 access token. Operations list the permissions they require as scopes.",
        });

        return Task.CompletedTask;
    }
}
