using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Publishes events to NATS JetStream.
/// The stream "tukifact-events" is created by NatsConsumerHostedService on startup.
/// If JetStream publish fails (stream not ready), falls back to plain NATS publish.
/// </summary>
public class NatsEventPublisher : IEventPublisher
{
    private readonly string _natsUrl;
    private readonly ILogger<NatsEventPublisher> _logger;

    public NatsEventPublisher(IConfiguration configuration, ILogger<NatsEventPublisher> logger)
    {
        _natsUrl = configuration["Nats:Url"] ?? "nats://localhost:4222";
        _logger = logger;
    }

    public async Task PublishAsync<T>(string subject, T eventData, CancellationToken ct = default) where T : class
    {
        try
        {
            var opts = new NatsOpts { Url = _natsUrl };
            await using var nats = new NatsConnection(opts);
            await nats.ConnectAsync();

            var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                // Try JetStream publish first (durable, guaranteed delivery)
                var js = new NatsJSContext(nats);
                var ack = await js.PublishAsync(subject, bytes, cancellationToken: ct);
                ack.EnsureSuccess();
                _logger.LogInformation("Published JetStream event to {Subject}: {Type}", subject, typeof(T).Name);
            }
            catch (Exception jsEx)
            {
                // Fallback to plain NATS if JetStream stream not ready yet
                _logger.LogWarning(jsEx, "JetStream publish failed for {Subject}, falling back to plain NATS", subject);
                await nats.PublishAsync(subject, bytes, cancellationToken: ct);
                _logger.LogInformation("Published plain NATS event to {Subject}: {Type}", subject, typeof(T).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to {Subject}", subject);
            // Don't throw — events are fire-and-forget, shouldn't break the main flow
        }
    }
}
