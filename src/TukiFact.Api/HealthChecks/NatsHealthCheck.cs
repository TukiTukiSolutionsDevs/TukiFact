using Microsoft.Extensions.Diagnostics.HealthChecks;
using NATS.Net;

namespace TukiFact.Api.Middleware;

public class NatsHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public NatsHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var natsUrl = _configuration.GetValue<string>("Nats:Url") ?? "nats://localhost:4222";

            // NatsClient(url) — NATS.Net v2 simplified API
            await using var client = new NatsClient(natsUrl);
            await client.ConnectAsync();

            // PingAsync returns TimeSpan (RTT) — proves connectivity
            var rtt = await client.PingAsync(cancellationToken);

            return HealthCheckResult.Healthy($"NATS is reachable. RTT: {rtt.TotalMilliseconds:F1}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("NATS is unreachable", ex);
        }
    }
}
