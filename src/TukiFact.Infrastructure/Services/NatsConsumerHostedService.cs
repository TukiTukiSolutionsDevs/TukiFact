using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// BackgroundService that consumes events from NATS JetStream.
/// Creates the stream + durable consumer on startup, then loops consuming messages
/// and dispatching to the appropriate IEventHandler.
/// </summary>
public class NatsConsumerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NatsConsumerHostedService> _logger;
    private readonly string _natsUrl;

    private const string StreamName = "tukifact-events";
    private const string ConsumerName = "tukifact-worker";

    private static readonly string[] AllSubjects =
    [
        "document.created",
        "document.sent",
        "document.failed",
        "document.voided",
        "quotation.created",
        "quotation.converted",
        "retention.created",
        "perception.created",
        "despatch.emitted"
    ];

    public NatsConsumerHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<NatsConsumerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _natsUrl = configuration["Nats:Url"] ?? "nats://localhost:4222";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the app to fully start before connecting
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        _logger.LogInformation("NATS Consumer starting — connecting to {Url}", _natsUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("NATS Consumer shutting down gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NATS Consumer error — reconnecting in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeLoopAsync(CancellationToken ct)
    {
        var opts = new NatsOpts { Url = _natsUrl };
        await using var nats = new NatsConnection(opts);
        await nats.ConnectAsync();

        var js = new NatsJSContext(nats);

        // Create or update the JetStream stream
        var streamConfig = new StreamConfig(StreamName, AllSubjects)
        {
            Retention = StreamConfigRetention.Limits,
            MaxAge = TimeSpan.FromDays(7),
            Storage = StreamConfigStorage.File,
            NumReplicas = 1,
            DuplicateWindow = TimeSpan.FromMinutes(2)
        };
        await js.CreateOrUpdateStreamAsync(streamConfig, ct);
        _logger.LogInformation("JetStream stream '{Stream}' ready with {Count} subjects", StreamName, AllSubjects.Length);

        // Create or update durable consumer
        var consumerConfig = new ConsumerConfig(ConsumerName)
        {
            DurableName = ConsumerName,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            AckWait = TimeSpan.FromSeconds(30),
            MaxDeliver = 5,
            FilterSubjects = AllSubjects.ToList()
        };
        var consumer = await js.CreateOrUpdateConsumerAsync(StreamName, consumerConfig, ct);
        _logger.LogInformation("JetStream consumer '{Consumer}' ready", ConsumerName);

        // Consume loop — process messages forever
        await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: ct))
        {
            var subject = msg.Subject;
            try
            {
                _logger.LogDebug("Received event: {Subject} ({Size} bytes)", subject, msg.Data?.Length ?? 0);

                using var scope = _serviceProvider.CreateScope();
                var handlers = scope.ServiceProvider.GetServices<IEventHandler>();

                var matched = false;
                foreach (var handler in handlers)
                {
                    if (handler.Subjects.Any(s => s == subject))
                    {
                        await handler.HandleAsync(subject, msg.Data ?? [], ct);
                        matched = true;
                    }
                }

                if (!matched)
                    _logger.LogWarning("No handler registered for subject: {Subject}", subject);

                await msg.AckAsync(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {Subject} — will be redelivered", subject);
                await msg.NakAsync(delay: TimeSpan.FromSeconds(10), cancellationToken: ct);
            }
        }
    }
}
