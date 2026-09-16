using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TukiFact.Common.Presentation.DependencyInjection;
using TukiFact.Common.Presentation.Grpc;
using TukiFact.Common.Presentation.Grpc.Interceptors;

namespace TukiFact.Common.UnitTests.Presentation.Grpc;

/// <summary>Composition tests for <see cref="GrpcDependencyInjection.AddCommonGrpc"/>.</summary>
public sealed class GrpcDependencyInjectionTests
{
    [Fact]
    public void AddCommonGrpc_NoOverride_RegistersTheNullDetailFactoryByDefault()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCommonGrpc();

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IRpcErrorDetailFactory>().Should().BeOfType<NullRpcErrorDetailFactory>();
    }

    [Fact]
    public void AddCommonGrpc_Always_AddsCorrelationBeforeExceptionInterceptor()
    {
        // The correlation id must already be set (in trailers) before the exception interceptor
        // builds a Status, so registration order is load-bearing, not incidental.
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCommonGrpc();

        // Assert
        using var provider = services.BuildServiceProvider();
        var grpcOptions = provider.GetRequiredService<IOptions<GrpcServiceOptions>>().Value;
        var interceptorTypes = grpcOptions.Interceptors.Select(registration => registration.Type).ToArray();

        interceptorTypes.Should().ContainInOrder(typeof(CorrelationIdInterceptor), typeof(ExceptionInterceptor));
    }
}
