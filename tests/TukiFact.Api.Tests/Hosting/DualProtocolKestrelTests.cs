using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TukiFact.Api.Hosting;

namespace TukiFact.Api.Tests.Hosting;

/// <summary>
/// <see cref="DualProtocolKestrel.Resolve"/> is a pure config→endpoint mapping:
/// default (or explicitly disabled) means no explicit Kestrel
/// endpoint, so <c>ASPNETCORE_URLS</c> keeps driving the binding exactly as it does today.
/// </summary>
public sealed class DualProtocolKestrelTests
{
    [Fact]
    public void Resolve_GrpcNotConfigured_ReturnsNull()
    {
        var configuration = BuildConfiguration();

        var endpoints = DualProtocolKestrel.Resolve(configuration);

        endpoints.Should().BeNull();
    }

    [Fact]
    public void Resolve_GrpcExplicitlyDisabled_ReturnsNull()
    {
        var configuration = BuildConfiguration(("Grpc:Enabled", "false"));

        var endpoints = DualProtocolKestrel.Resolve(configuration);

        endpoints.Should().BeNull();
    }

    [Fact]
    public void Resolve_GrpcEnabledWithNoPortsConfigured_ReturnsTheDefaultPorts()
    {
        var configuration = BuildConfiguration(("Grpc:Enabled", "true"));

        var endpoints = DualProtocolKestrel.Resolve(configuration);

        endpoints.Should().NotBeNull();
        endpoints!.HttpPort.Should().Be(8080);
        endpoints.GrpcPort.Should().Be(8081);
    }

    [Fact]
    public void Resolve_GrpcEnabledWithExplicitPorts_ReturnsThem()
    {
        var configuration = BuildConfiguration(("Grpc:Enabled", "true"), ("Http:Port", "9090"), ("Grpc:Port", "9091"));

        var endpoints = DualProtocolKestrel.Resolve(configuration);

        endpoints.Should().NotBeNull();
        endpoints!.HttpPort.Should().Be(9090);
        endpoints.GrpcPort.Should().Be(9091);
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] entries) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(entry => new KeyValuePair<string, string?>(entry.Key, entry.Value)))
            .Build();
}
