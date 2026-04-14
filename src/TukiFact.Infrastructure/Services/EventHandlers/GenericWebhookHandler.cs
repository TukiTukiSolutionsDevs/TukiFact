using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services.EventHandlers;

/// <summary>
/// Generic handler for events that trigger webhooks + logging.
/// Covers: document.voided, quotation.created, quotation.converted,
/// retention.created, perception.created, despatch.emitted.
/// </summary>
public class GenericWebhookHandler : IEventHandler
{
    private readonly WebhookDeliveryService _webhookService;
    private readonly ILogger<GenericWebhookHandler> _logger;

    public GenericWebhookHandler(
        WebhookDeliveryService webhookService,
        ILogger<GenericWebhookHandler> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    public IReadOnlyList<string> Subjects =>
    [
        "document.voided",
        "quotation.created",
        "quotation.converted",
        "retention.created",
        "perception.created",
        "despatch.emitted"
    ];

    public async Task HandleAsync(string subject, byte[] payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<TukiFactEvent>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt is null)
        {
            _logger.LogWarning("Failed to deserialize {Subject} event", subject);
            return;
        }

        _logger.LogInformation(
            "Event {Subject}: {EntityType} {EntityId} for tenant {TenantId}",
            subject, evt.EntityType, evt.EntityId, evt.TenantId);

        // Dispatch webhook
        try
        {
            var webhookPayload = new
            {
                @event = subject,
                timestamp = evt.Timestamp,
                data = new
                {
                    entity_id = evt.EntityId,
                    entity_type = evt.EntityType,
                    document_type = evt.DocumentType,
                    serie = evt.Serie,
                    correlativo = evt.Correlative,
                    full_number = evt.FullNumber,
                    total = evt.Total,
                    currency = evt.Currency,
                    status = evt.Status,
                    customer_name = evt.CustomerName,
                    sunat_response_code = evt.SunatResponseCode
                }
            };

            await _webhookService.DeliverEventAsync(evt.TenantId, subject, webhookPayload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch webhook for {Subject} event", subject);
        }
    }
}
