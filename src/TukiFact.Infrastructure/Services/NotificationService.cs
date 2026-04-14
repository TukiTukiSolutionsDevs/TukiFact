using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Manages in-app notifications and SSE (Server-Sent Events) broadcasting.
/// Maintains a dictionary of active SSE connections per tenant.
/// When a notification is created, it's saved to DB and broadcast to all
/// connected SSE clients for that tenant.
/// </summary>
public class NotificationService
{
    private readonly INotificationRepository _repo;
    private readonly ILogger<NotificationService> _logger;

    // TenantId → list of SSE response streams
    private static readonly ConcurrentDictionary<Guid, ConcurrentBag<SseClient>> _sseClients = new();

    public NotificationService(INotificationRepository repo, ILogger<NotificationService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// Create a notification, save to DB, and broadcast via SSE to connected clients.
    /// </summary>
    public async Task<Notification> CreateAndBroadcastAsync(
        Guid tenantId, string type, string title, string? body = null,
        string? entityType = null, Guid? entityId = null, Guid? userId = null,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            TenantId = tenantId,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false
        };

        await _repo.CreateAsync(notification, ct);
        _logger.LogInformation("Notification created: [{Type}] {Title} for tenant {TenantId}", type, title, tenantId);

        // Broadcast to SSE clients
        await BroadcastToTenantAsync(tenantId, notification, ct);

        return notification;
    }

    /// <summary>
    /// Register an SSE client for a tenant. Returns a disposable handle.
    /// </summary>
    public SseClient RegisterClient(Guid tenantId, HttpResponse response)
    {
        var client = new SseClient(tenantId, response);
        var bag = _sseClients.GetOrAdd(tenantId, _ => new ConcurrentBag<SseClient>());
        bag.Add(client);
        _logger.LogDebug("SSE client registered for tenant {TenantId}. Total: {Count}", tenantId, bag.Count);
        return client;
    }

    /// <summary>
    /// Remove a disconnected SSE client.
    /// </summary>
    public void UnregisterClient(SseClient client)
    {
        if (_sseClients.TryGetValue(client.TenantId, out var bag))
        {
            // ConcurrentBag doesn't support removal — rebuild without the dead client
            var alive = new ConcurrentBag<SseClient>(bag.Where(c => c != client && !c.IsDisconnected));
            _sseClients[client.TenantId] = alive;
            _logger.LogDebug("SSE client unregistered for tenant {TenantId}. Remaining: {Count}", client.TenantId, alive.Count);
        }
    }

    private async Task BroadcastToTenantAsync(Guid tenantId, Notification notification, CancellationToken ct)
    {
        if (!_sseClients.TryGetValue(tenantId, out var clients)) return;

        var json = JsonSerializer.Serialize(new
        {
            id = notification.Id,
            type = notification.Type,
            title = notification.Title,
            body = notification.Body,
            entityType = notification.EntityType,
            entityId = notification.EntityId,
            isRead = notification.IsRead,
            createdAt = notification.CreatedAt
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var sseData = $"event: notification\ndata: {json}\n\n";
        var deadClients = new List<SseClient>();

        foreach (var client in clients)
        {
            try
            {
                if (client.IsDisconnected)
                {
                    deadClients.Add(client);
                    continue;
                }
                await client.Response.WriteAsync(sseData, ct);
                await client.Response.Body.FlushAsync(ct);
            }
            catch
            {
                deadClients.Add(client);
            }
        }

        // Clean up dead connections
        foreach (var dead in deadClients)
            UnregisterClient(dead);
    }
}

/// <summary>
/// Represents a connected SSE client for a specific tenant.
/// </summary>
public class SseClient
{
    public Guid TenantId { get; }
    public HttpResponse Response { get; }
    public bool IsDisconnected { get; set; }

    public SseClient(Guid tenantId, HttpResponse response)
    {
        TenantId = tenantId;
        Response = response;
    }
}
