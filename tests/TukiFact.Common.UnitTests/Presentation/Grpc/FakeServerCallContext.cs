using Grpc.Core;

namespace TukiFact.Common.UnitTests.Presentation.Grpc;

/// <summary>
/// Minimal <see cref="ServerCallContext"/> test double. <c>Grpc.Core.Testing</c> targets the legacy
/// C-core package family (stale since 2.46.6, superseded by grpc-dotnet/<c>Grpc.AspNetCore.Server</c>
/// years ago) and is not added here to avoid mixing package families; grpc-dotnet ships no test
/// double of its own, so interceptor tests build one against the small abstract surface instead.
/// </summary>
internal sealed class FakeServerCallContext : ServerCallContext
{
    public FakeServerCallContext(Metadata? requestHeaders = null) =>
        RequestHeadersCore = requestHeaders ?? [];

    protected override string MethodCore => "/tukifact.v1.CpeService/GetDocument";

    protected override string HostCore => "localhost";

    protected override string PeerCore => "ipv4:127.0.0.1";

    protected override DateTime DeadlineCore => DateTime.UtcNow.AddSeconds(30);

    protected override Metadata RequestHeadersCore { get; }

    protected override CancellationToken CancellationTokenCore => CancellationToken.None;

    protected override Metadata ResponseTrailersCore { get; } = [];

    protected override Status StatusCore { get; set; }

    protected override WriteOptions? WriteOptionsCore { get; set; }

    protected override AuthContext AuthContextCore { get; } = new("none", new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException("Not exercised by these tests.");

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}
