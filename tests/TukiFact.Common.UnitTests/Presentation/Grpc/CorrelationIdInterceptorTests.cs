using Grpc.Core;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Presentation.Grpc.Interceptors;

namespace TukiFact.Common.UnitTests.Presentation.Grpc;

/// <summary>
/// <see cref="CorrelationIdInterceptor"/> mirrors the HTTP-side <c>CorrelationIdMiddleware</c>:
/// trailers, not headers, are the only metadata carrier present on
/// both a successful and a failing gRPC call, so the correlation id is echoed there.
/// </summary>
public sealed class CorrelationIdInterceptorTests
{
    [Fact]
    public async Task UnaryServerHandler_NoIncomingHeader_GeneratesOneAndEchoesItInTrailers()
    {
        // Arrange
        var correlation = new FakeCorrelationIdAccessor();
        var interceptor = new CorrelationIdInterceptor(correlation);
        var context = new FakeServerCallContext();

        // Act
        await interceptor.UnaryServerHandler("request", context, (_, _) => Task.FromResult("response"));

        // Assert
        var echoed = context.ResponseTrailers.GetValue(CorrelationIdInterceptor.MetadataKey);
        echoed.Should().NotBeNullOrWhiteSpace();
        echoed.Should().Be(correlation.CorrelationId);
    }

    [Fact]
    public async Task UnaryServerHandler_WellFormedIncomingHeader_IsKeptAndEchoed()
    {
        // Arrange
        var correlation = new FakeCorrelationIdAccessor();
        var interceptor = new CorrelationIdInterceptor(correlation);
        var headers = new Metadata { { CorrelationIdInterceptor.MetadataKey, "req-abc-123" } };
        var context = new FakeServerCallContext(headers);

        // Act
        await interceptor.UnaryServerHandler("request", context, (_, _) => Task.FromResult("response"));

        // Assert
        correlation.CorrelationId.Should().Be("req-abc-123");
        context.ResponseTrailers.GetValue(CorrelationIdInterceptor.MetadataKey).Should().Be("req-abc-123");
    }

    [Fact]
    public async Task UnaryServerHandler_MalformedIncomingHeader_IsIgnoredAndReplacedWithAGeneratedId()
    {
        // Arrange
        var correlation = new FakeCorrelationIdAccessor();
        var interceptor = new CorrelationIdInterceptor(correlation);
        var headers = new Metadata { { CorrelationIdInterceptor.MetadataKey, "has spaces / slash" } };
        var context = new FakeServerCallContext(headers);

        // Act
        await interceptor.UnaryServerHandler("request", context, (_, _) => Task.FromResult("response"));

        // Assert
        var echoed = context.ResponseTrailers.GetValue(CorrelationIdInterceptor.MetadataKey);
        echoed.Should().NotBe("has spaces / slash");
    }

    [Fact]
    public async Task UnaryServerHandler_FailingContinuation_StillEchoesTheCorrelationIdInTrailers()
    {
        // The correlation id must reach trailers on both outcomes, so it is set before the
        // continuation runs, not after it returns.
        // Arrange
        var correlation = new FakeCorrelationIdAccessor();
        var interceptor = new CorrelationIdInterceptor(correlation);
        var context = new FakeServerCallContext();

        // Act
        var act = () => interceptor.UnaryServerHandler<string, string>(
            "request", context, (_, _) => throw new InvalidOperationException("boom"));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        context.ResponseTrailers.GetValue(CorrelationIdInterceptor.MetadataKey).Should().NotBeNullOrWhiteSpace();
    }

    internal sealed class FakeCorrelationIdAccessor : ICorrelationIdAccessor
    {
        private string? _correlationId;

        public string CorrelationId => _correlationId ??= Guid.NewGuid().ToString("N");

        public void Set(string correlationId) => _correlationId = correlationId;
    }
}
