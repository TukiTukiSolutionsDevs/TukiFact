using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TukiFact.Common.Domain.Rules;
using TukiFact.Common.Presentation.Grpc;
using TukiFact.Common.Presentation.Grpc.Interceptors;

namespace TukiFact.Common.UnitTests.Presentation.Grpc;

/// <summary>
/// <see cref="ExceptionInterceptor"/> never lets a raw exception escape to a gRPC client.
/// It reuses <see cref="RpcErrorMapping"/> so the mapped status
/// follows the same rules as an explicit <c>Result</c> failure.
/// </summary>
public sealed class ExceptionInterceptorTests
{
    [Fact]
    public async Task UnaryServerHandler_BusinessRuleValidationException_MapsToAbortedWithTheRuleMessage()
    {
        // Arrange
        var interceptor = new ExceptionInterceptor(new NullRpcErrorDetailFactory(), NullLogger<ExceptionInterceptor>.Instance);
        var context = new FakeServerCallContext();
        var rule = new FakeBusinessRule("Order cannot ship without items.");

        // Act
        var act = () => interceptor.UnaryServerHandler<string, string>(
            "request", context, (_, _) => throw new BusinessRuleValidationException(rule));

        // Assert
        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.Aborted);
        exception.Which.Status.Detail.Should().Be(rule.Message);
    }

    [Fact]
    public async Task UnaryServerHandler_DbUpdateConcurrencyException_MapsToAbortedWithTheConcurrencyDetail()
    {
        // Common.Presentation cannot reference Microsoft.EntityFrameworkCore
        // (Presentation may not reference a data package), so the interceptor matches this by type name; the real EF type is
        // used here (available transitively through TukiFact.Common.Infrastructure) to prove the
        // match is not a coincidence of a look-alike fake.
        // Arrange
        var interceptor = new ExceptionInterceptor(new NullRpcErrorDetailFactory(), NullLogger<ExceptionInterceptor>.Instance);
        var context = new FakeServerCallContext();

        // Act
        var act = () => interceptor.UnaryServerHandler<string, string>(
            "request", context, (_, _) => throw new DbUpdateConcurrencyException("optimistic concurrency failure"));

        // Assert
        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.Aborted);
        exception.Which.Status.Detail.Should().Be("The record changed.");
    }

    [Fact]
    public async Task UnaryServerHandler_UnknownException_MapsToInternalAndNeverLeaksTheMessage()
    {
        // Arrange
        var interceptor = new ExceptionInterceptor(new NullRpcErrorDetailFactory(), NullLogger<ExceptionInterceptor>.Instance);
        var context = new FakeServerCallContext();

        // Act
        var act = () => interceptor.UnaryServerHandler<string, string>(
            "request", context, (_, _) => throw new InvalidOperationException("NullReferenceException at Foo.Bar line 42"));

        // Assert
        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.Internal);
        exception.Which.Status.Detail.Should().NotContain("NullReferenceException");
    }

    [Fact]
    public async Task UnaryServerHandler_AlreadyAnRpcException_IsRethrownUnchanged()
    {
        // A deliberate RpcException (e.g. raised by application code via RpcErrorMapping) already
        // carries the status the caller intended; it must not be re-wrapped as INTERNAL.
        // Arrange
        var interceptor = new ExceptionInterceptor(new NullRpcErrorDetailFactory(), NullLogger<ExceptionInterceptor>.Instance);
        var context = new FakeServerCallContext();
        var original = new RpcException(new Status(StatusCode.NotFound, "Document not found."));

        // Act
        var act = () => interceptor.UnaryServerHandler<string, string>("request", context, (_, _) => throw original);

        // Assert
        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.Should().BeSameAs(original);
    }

    [Fact]
    public async Task UnaryServerHandler_SuccessfulContinuation_ReturnsItsResultUnchanged()
    {
        // Arrange
        var interceptor = new ExceptionInterceptor(new NullRpcErrorDetailFactory(), NullLogger<ExceptionInterceptor>.Instance);
        var context = new FakeServerCallContext();

        // Act
        var response = await interceptor.UnaryServerHandler("request", context, (_, _) => Task.FromResult("ok"));

        // Assert
        response.Should().Be("ok");
    }

    private sealed class FakeBusinessRule(string message) : IBusinessRule
    {
        public string Message { get; } = message;

        public bool IsBroken() => true;
    }
}
