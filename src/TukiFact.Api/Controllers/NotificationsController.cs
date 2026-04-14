using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Interfaces;
using TukiFact.Infrastructure.Services;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _repo;
    private readonly NotificationService _notificationService;
    private readonly ITenantProvider _tenantProvider;

    public NotificationsController(
        INotificationRepository repo,
        NotificationService notificationService,
        ITenantProvider tenantProvider)
    {
        _repo = repo;
        _notificationService = notificationService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get paginated list of notifications for the current tenant.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var notifications = await _repo.GetByTenantAsync(tenantId, page, pageSize, ct);
        var unreadCount = await _repo.GetUnreadCountAsync(tenantId, ct);

        return Ok(new
        {
            data = notifications.Select(n => new
            {
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.EntityType,
                n.EntityId,
                n.IsRead,
                n.CreatedAt
            }),
            unreadCount,
            page,
            pageSize
        });
    }

    /// <summary>
    /// SSE endpoint — Server-Sent Events stream for real-time notifications.
    /// Client opens a persistent connection and receives events as they happen.
    /// </summary>
    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // Disable nginx buffering

        // Send initial ping
        await Response.WriteAsync($"event: ping\ndata: {{\"connected\":true,\"tenantId\":\"{tenantId}\"}}\n\n", ct);
        await Response.Body.FlushAsync(ct);

        // Register this connection for SSE broadcasting
        var client = _notificationService.RegisterClient(tenantId, Response);

        try
        {
            // Keep connection alive until client disconnects
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                // Send keepalive ping every 30 seconds
                await Response.WriteAsync(": keepalive\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — expected
        }
        finally
        {
            client.IsDisconnected = true;
            _notificationService.UnregisterClient(client);
        }
    }

    /// <summary>
    /// Mark a specific notification as read.
    /// </summary>
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        await _repo.MarkAsReadAsync(id, tenantId, ct);
        return NoContent();
    }

    /// <summary>
    /// Mark all notifications as read.
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        await _repo.MarkAllAsReadAsync(tenantId, ct);
        return NoContent();
    }

    /// <summary>
    /// Get unread notification count (for badge).
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var count = await _repo.GetUnreadCountAsync(tenantId, ct);
        return Ok(new { count });
    }
}
