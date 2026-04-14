using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services.EventHandlers;

/// <summary>
/// Creates in-app notifications for ALL event types.
/// Broadcasts via SSE to connected clients in real-time.
/// </summary>
public class NotificationEventHandler : IEventHandler
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<NotificationEventHandler> _logger;

    public NotificationEventHandler(
        NotificationService notificationService,
        ILogger<NotificationEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public IReadOnlyList<string> Subjects =>
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

    public async Task HandleAsync(string subject, byte[] payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<TukiFactEvent>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt is null)
        {
            _logger.LogWarning("Failed to deserialize {Subject} event for notification", subject);
            return;
        }

        var (title, body) = BuildNotificationContent(subject, evt);

        try
        {
            await _notificationService.CreateAndBroadcastAsync(
                tenantId: evt.TenantId,
                type: subject,
                title: title,
                body: body,
                entityType: evt.EntityType,
                entityId: evt.EntityId,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for {Subject}", subject);
        }
    }

    private static (string Title, string? Body) BuildNotificationContent(string subject, TukiFactEvent evt)
    {
        return subject switch
        {
            "document.created" => (
                $"Documento {evt.FullNumber} creado",
                $"{GetDocTypeName(evt.DocumentType)} por {evt.Currency} {evt.Total:N2}"
            ),
            "document.sent" => (
                $"{GetDocTypeName(evt.DocumentType)} {evt.FullNumber} enviada a SUNAT",
                evt.SunatResponseCode == "0"
                    ? "Aceptada correctamente"
                    : $"Respuesta: {evt.SunatResponseCode} — {evt.SunatResponseDescription}"
            ),
            "document.failed" => (
                $"ERROR: {evt.FullNumber} rechazado por SUNAT",
                $"Codigo: {evt.SunatResponseCode} — {evt.ErrorMessage ?? evt.SunatResponseDescription}"
            ),
            "document.voided" => (
                $"Documento {evt.FullNumber} anulado",
                "Comunicacion de baja enviada a SUNAT"
            ),
            "quotation.created" => (
                $"Cotizacion creada: {evt.FullNumber}",
                $"Total: {evt.Currency} {evt.Total:N2} — Cliente: {evt.CustomerName}"
            ),
            "quotation.converted" => (
                $"Cotizacion {evt.FullNumber} convertida a factura",
                null
            ),
            "retention.created" => (
                $"Retencion creada: {evt.FullNumber}",
                $"Total retenido: {evt.Currency} {evt.Total:N2}"
            ),
            "perception.created" => (
                $"Percepcion creada: {evt.FullNumber}",
                $"Total percibido: {evt.Currency} {evt.Total:N2}"
            ),
            "despatch.emitted" => (
                $"Guia de remision {evt.FullNumber} emitida",
                "Enviada a SUNAT via GRE API"
            ),
            _ => ($"Evento: {subject}", evt.FullNumber)
        };
    }

    private static string GetDocTypeName(string? docType) => docType switch
    {
        "01" => "Factura",
        "03" => "Boleta",
        "07" => "Nota de Credito",
        "08" => "Nota de Debito",
        _ => "Documento"
    };
}
