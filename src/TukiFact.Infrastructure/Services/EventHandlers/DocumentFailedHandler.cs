using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services.EventHandlers;

/// <summary>
/// Handles document.failed events.
/// Actions: webhook dispatch, notification (urgent), log error details.
/// </summary>
public class DocumentFailedHandler : IEventHandler
{
    private readonly WebhookDeliveryService _webhookService;
    private readonly ILogger<DocumentFailedHandler> _logger;

    public DocumentFailedHandler(
        WebhookDeliveryService webhookService,
        ILogger<DocumentFailedHandler> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    public IReadOnlyList<string> Subjects => ["document.failed"];

    public async Task HandleAsync(string subject, byte[] payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<TukiFactEvent>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt is null)
        {
            _logger.LogWarning("Failed to deserialize document.failed event");
            return;
        }

        _logger.LogError(
            "Document FAILED: {FullNumber} — SUNAT Code: {Code}, Error: {Error}",
            evt.FullNumber, evt.SunatResponseCode, evt.ErrorMessage ?? evt.SunatResponseDescription);

        // Dispatch webhook
        try
        {
            var webhookPayload = new
            {
                @event = "document.failed",
                timestamp = evt.Timestamp,
                data = new
                {
                    document_id = evt.EntityId,
                    type = evt.DocumentType,
                    serie = evt.Serie,
                    correlativo = evt.Correlative,
                    full_number = evt.FullNumber,
                    total = evt.Total,
                    currency = evt.Currency,
                    status = "failed",
                    error_message = evt.ErrorMessage,
                    sunat_response_code = evt.SunatResponseCode,
                    sunat_response_description = evt.SunatResponseDescription
                }
            };

            await _webhookService.DeliverEventAsync(evt.TenantId, "document.failed", webhookPayload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch webhook for failed document {FullNumber}", evt.FullNumber);
        }
    }
}
