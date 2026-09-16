using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using TukiFact.Api.OpenApi;
using TukiFact.Common.Presentation.Endpoints;

namespace TukiFact.Api.Tests.OpenApi;

/// <summary>
/// <see cref="OperationIdTransformer"/> against a hand-built <see cref="OpenApiOperationTransformerContext"/>:
/// no metadata at all is a legacy MVC controller and is left alone; a module endpoint that forgot
/// <c>WithName</c> fails the document generation (and therefore the build) naming the endpoint;
/// module + use case yields <c>Module_UseCase</c>.
/// </summary>
public sealed class OperationIdTransformerTests
{
    private const string FrameworkOperationId = "GetDocumentsById";

    [Fact]
    public async Task TransformAsync_NoModuleAndNoUseCaseMetadata_KeepsTheFrameworkOperationId()
    {
        // Arrange
        var operation = new OpenApiOperation { OperationId = FrameworkOperationId };
        var context = ContextWithMetadata();

        // Act
        await new OperationIdTransformer().TransformAsync(operation, context, CancellationToken.None);

        // Assert
        operation.OperationId.Should().Be(FrameworkOperationId);
    }

    [Fact]
    public async Task TransformAsync_ModuleWithoutUseCaseName_ThrowsNamingTheEndpoint()
    {
        // Arrange
        var operation = new OpenApiOperation { OperationId = FrameworkOperationId };
        var context = ContextWithMetadata(new EndpointModuleMetadata("Invoicing"));

        // Act
        var act = () => new OperationIdTransformer().TransformAsync(operation, context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*POST api/v1/invoices*")
            .WithMessage("*Invoicing*")
            .WithMessage("*WithName*");
    }

    [Fact]
    public async Task TransformAsync_ModuleAndUseCaseName_SetsModuleUnderscoreUseCase()
    {
        // Arrange
        var operation = new OpenApiOperation { OperationId = FrameworkOperationId };
        var context = ContextWithMetadata(new EndpointModuleMetadata("Invoicing"), new EndpointNameMetadata("EmitInvoice"));

        // Act
        await new OperationIdTransformer().TransformAsync(operation, context, CancellationToken.None);

        // Assert
        operation.OperationId.Should().Be("Invoicing_EmitInvoice");
    }

    private static OpenApiOperationTransformerContext ContextWithMetadata(params object[] endpointMetadata) =>
        new()
        {
            DocumentName = "v1",
            Description = new ApiDescription
            {
                HttpMethod = "POST",
                RelativePath = "api/v1/invoices",
                ActionDescriptor = new ActionDescriptor { EndpointMetadata = endpointMetadata },
            },
            ApplicationServices = new ServiceCollection().BuildServiceProvider(),
        };
}
