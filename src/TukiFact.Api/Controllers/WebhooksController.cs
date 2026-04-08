using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Webhooks;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/webhooks")]
[Authorize(Roles = "admin")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookRepository _webhookRepo;
    private readonly IWebhookDeliveryRepository _deliveryRepo;
    private readonly ITenantProvider _tenantProvider;

    public WebhooksController(IWebhookRepository webhookRepo, IWebhookDeliveryRepository deliveryRepo, ITenantProvider tenantProvider)
    {
        _webhookRepo = webhookRepo;
        _deliveryRepo = deliveryRepo;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var webhooks = await _webhookRepo.GetByTenantAsync(tenantId, ct);
        return Ok(webhooks.Select(w => new WebhookConfigResponse(
            w.Id, w.Url, JsonSerializer.Deserialize<string[]>(w.Events) ?? [], w.IsActive,
            w.MaxRetries, w.LastTriggeredAt, w.CreatedAt)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        var config = new WebhookConfig
        {
            TenantId = tenantId,
            Url = request.Url,
            Secret = secret,
            Events = JsonSerializer.Serialize(request.Events),
            MaxRetries = request.MaxRetries
        };
        await _webhookRepo.CreateAsync(config, ct);

        return Created($"/v1/webhooks/{config.Id}", new
        {
            config.Id, config.Url, Events = request.Events, config.IsActive,
            config.MaxRetries, Secret = secret, // Only shown on creation
            config.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWebhookRequest request, CancellationToken ct)
    {
        var config = await _webhookRepo.GetByIdAsync(id, ct);
        if (config is null) return NotFound();

        if (request.Url is not null) config.Url = request.Url;
        if (request.Events is not null) config.Events = JsonSerializer.Serialize(request.Events);
        if (request.IsActive.HasValue) config.IsActive = request.IsActive.Value;
        if (request.MaxRetries.HasValue) config.MaxRetries = request.MaxRetries.Value;

        await _webhookRepo.UpdateAsync(config, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _webhookRepo.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/deliveries")]
    public async Task<IActionResult> GetDeliveries(Guid id, CancellationToken ct)
    {
        var deliveries = await _deliveryRepo.GetByWebhookAsync(id, 50, ct);
        return Ok(deliveries.Select(d => new WebhookDeliveryResponse(d.Id, d.EventType, d.Status, d.Attempt, d.ResponseStatus, d.CreatedAt)));
    }
}
