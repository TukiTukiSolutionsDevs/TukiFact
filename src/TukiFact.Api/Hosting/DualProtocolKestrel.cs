using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace TukiFact.Api.Hosting;

/// <summary>Explicit Kestrel endpoints when <see cref="DualProtocolKestrel.Resolve"/> finds gRPC enabled; <see langword="null"/> otherwise.</summary>
public sealed record DualProtocolEndpoints(int HttpPort, int GrpcPort);

/// <summary>
/// Binds Kestrel to two explicit ports only when <c>Grpc:Enabled=true</c> (default
/// <see langword="false"/>): HTTP/1.1 on <c>Http:Port</c> (8080) for the
/// existing REST/legacy surface, h2c (cleartext HTTP/2) on <c>Grpc:Port</c> (8081) for gRPC. Any
/// explicit <c>Listen*</c> call makes Kestrel stop honouring <c>ASPNETCORE_URLS</c>, so both ports
/// are always bound together, never just one. The default (disabled) leaves Kestrel's normal
/// <c>ASPNETCORE_URLS</c>-driven binding untouched — the default changes no production behaviour.
/// </summary>
public static class DualProtocolKestrel
{
    internal const string EnabledKey = "Grpc:Enabled";
    private const string HttpPortKey = "Http:Port";
    private const string GrpcPortKey = "Grpc:Port";
    private const int DefaultHttpPort = 8080;
    private const int DefaultGrpcPort = 8081;

    /// <summary>
    /// Pure config→endpoint mapping, factored out of the Kestrel wiring below so it is
    /// unit-testable without spinning up Kestrel.
    /// </summary>
    public static DualProtocolEndpoints? Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configuration.GetValue(EnabledKey, defaultValue: false))
        {
            return null;
        }

        return new DualProtocolEndpoints(
            configuration.GetValue(HttpPortKey, DefaultHttpPort),
            configuration.GetValue(GrpcPortKey, DefaultGrpcPort));
    }

    public static WebApplicationBuilder ConfigureDualProtocol(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoints = Resolve(builder.Configuration);
        if (endpoints is null)
        {
            return builder;
        }

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.ListenAnyIP(endpoints.HttpPort, listen => listen.Protocols = HttpProtocols.Http1);
            kestrel.ListenAnyIP(endpoints.GrpcPort, listen => listen.Protocols = HttpProtocols.Http2);
        });

        return builder;
    }
}
