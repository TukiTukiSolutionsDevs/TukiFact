using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Net;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

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
            await using var client = new NatsClient(_natsUrl);
            var json = JsonSerializer.Serialize(eventData);
            var bytes = Encoding.UTF8.GetBytes(json);

            await client.PublishAsync(subject, bytes, cancellationToken: ct);
            _logger.LogInformation("Published event to {Subject}: {Type}", subject, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to {Subject}", subject);
            // Don't throw — events are fire-and-forget, shouldn't break the main flow
        }
    }
}
