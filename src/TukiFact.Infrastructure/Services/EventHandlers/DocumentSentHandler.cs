using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Services.EventHandlers;

/// <summary>
/// Handles document.sent events.
/// Actions: auto-email PDF (if tenant config), webhook dispatch, notification.
/// </summary>
public class DocumentSentHandler : IEventHandler
{
    private readonly IEmailService _emailService;
    private readonly WebhookDeliveryService _webhookService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DocumentSentHandler> _logger;

    public DocumentSentHandler(
        IEmailService emailService,
        WebhookDeliveryService webhookService,
        AppDbContext dbContext,
        ILogger<DocumentSentHandler> logger)
    {
        _emailService = emailService;
        _webhookService = webhookService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public IReadOnlyList<string> Subjects => ["document.sent"];

    public async Task HandleAsync(string subject, byte[] payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<TukiFactEvent>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt is null)
        {
            _logger.LogWarning("Failed to deserialize document.sent event");
            return;
        }

        _logger.LogInformation(
            "Document sent to SUNAT: {FullNumber} — Response: {Code} {Description}",
            evt.FullNumber, evt.SunatResponseCode, evt.SunatResponseDescription);

        // 1. Auto-email PDF if tenant has it enabled
        await TrySendEmailAsync(evt, ct);

        // 2. Dispatch webhooks
        await TryDispatchWebhookAsync(evt, ct);
    }

    private async Task TrySendEmailAsync(TukiFactEvent evt, CancellationToken ct)
    {
        try
        {
            // Check if tenant has auto-email enabled
            var tenantConfig = _dbContext.TenantServiceConfigs
                .FirstOrDefault(c => c.TenantId == evt.TenantId);

            if (tenantConfig is null || !tenantConfig.AutoSendEmail)
            {
                _logger.LogDebug("Auto-email disabled for tenant {TenantId}", evt.TenantId);
                return;
            }

            // Check if customer has email
            if (string.IsNullOrWhiteSpace(evt.CustomerEmail))
            {
                _logger.LogDebug("No customer email for document {FullNumber} — skipping email", evt.FullNumber);
                return;
            }

            await _emailService.SendDocumentEmailAsync(evt.TenantId, evt.EntityId, evt.CustomerEmail, ct);
            _logger.LogInformation("Auto-email sent for {FullNumber} to {Email}", evt.FullNumber, evt.CustomerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-send email for document {FullNumber}", evt.FullNumber);
            // Don't rethrow — email failure shouldn't block other handlers
        }
    }

    private async Task TryDispatchWebhookAsync(TukiFactEvent evt, CancellationToken ct)
    {
        try
        {
            var webhookPayload = new
            {
                @event = "document.sent",
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
                    status = evt.Status,
                    customer_name = evt.CustomerName,
                    sunat_response_code = evt.SunatResponseCode,
                    sunat_response_description = evt.SunatResponseDescription
                }
            };

            await _webhookService.DeliverEventAsync(evt.TenantId, "document.sent", webhookPayload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch webhook for document {FullNumber}", evt.FullNumber);
        }
    }
}
