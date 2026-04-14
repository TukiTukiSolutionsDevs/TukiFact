using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services.EventHandlers;

/// <summary>
/// Handles document.created events.
/// Actions: log creation + create in-app notification.
/// </summary>
public class DocumentCreatedHandler : IEventHandler
{
    private readonly ILogger<DocumentCreatedHandler> _logger;

    public DocumentCreatedHandler(ILogger<DocumentCreatedHandler> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> Subjects => ["document.created"];

    public Task HandleAsync(string subject, byte[] payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<TukiFactEvent>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt is null)
        {
            _logger.LogWarning("Failed to deserialize document.created event");
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Document created: {FullNumber} ({Type}) for tenant {TenantId} — Total: {Currency} {Total:N2}",
            evt.FullNumber, evt.DocumentType, evt.TenantId, evt.Currency, evt.Total);

        // Notification will be handled by NotificationHandler (M1.3)
        return Task.CompletedTask;
    }
}
