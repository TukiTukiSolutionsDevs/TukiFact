using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi;
using TukiFact.Common.Presentation.Endpoints;

namespace TukiFact.Api.OpenApi;

/// <summary>
/// Stable operationId <c>&lt;Module&gt;_&lt;UseCase&gt;</c> for an <see cref="IEndpoint"/> mapped
/// through <c>EndpointExtensions.MapEndpoints</c>: <see cref="EndpointModuleMetadata"/> comes from
/// the module group, <see cref="IEndpointNameMetadata"/> from the endpoint's <c>WithName</c>.
/// Three outcomes, by what the endpoint carries:
/// <list type="bullet">
/// <item>neither — a legacy MVC controller action: left untouched, the framework-generated
/// operationId stands;</item>
/// <item>module but no <c>WithName</c> — a module endpoint that forgot its use-case name: throws
/// <see cref="InvalidOperationException"/> naming the endpoint, which fails the build-time
/// document generation instead of silently shipping an unstable operationId;</item>
/// <item>both — <c>Module_UseCase</c>.</item>
/// </list>
/// </summary>
public sealed class OperationIdTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var module = metadata.OfType<EndpointModuleMetadata>().LastOrDefault();
        var useCase = metadata.OfType<IEndpointNameMetadata>().LastOrDefault();

        if (module is null)
        {
            return Task.CompletedTask;
        }

        if (useCase is null)
        {
            throw new InvalidOperationException(
                $"Endpoint '{context.Description.HttpMethod} {context.Description.RelativePath}' of module " +
                $"'{module.Name}' has no use-case name: every module endpoint must call WithName(...) so its " +
                "operationId is stable.");
        }

        operation.OperationId = $"{module.Name}_{useCase.EndpointName}";
        return Task.CompletedTask;
    }
}
