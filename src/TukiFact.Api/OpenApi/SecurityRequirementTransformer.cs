using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using TukiFact.Common.Presentation.Endpoints;

namespace TukiFact.Api.OpenApi;

/// <summary>
/// Bearer requirement only on operations that require a caller (<see cref="EndpointAccessMetadata"/>),
/// with the required permissions as scopes; anonymous operations, and the 33 legacy MVC controllers
/// (which never carry this metadata), carry none.
/// </summary>
public sealed class SecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var access = context.Description.ActionDescriptor.EndpointMetadata.OfType<EndpointAccessMetadata>().LastOrDefault();
        if (access is not { RequiresAuthentication: true })
        {
            return Task.CompletedTask;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(ApiInfoDocumentTransformer.BearerSchemeName, context.Document)] = [.. access.Permissions],
            },
        ];

        return Task.CompletedTask;
    }
}
