using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Application.DTOs.Billing;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

/// <summary>
/// Culqi recurring-billing flow. The tenant picks a plan, the frontend tokenizes
/// the card via Culqi.js, and we orchestrate customer + card + subscription on Culqi.
/// Webhooks reconcile state when Culqi charges the recurring fee.
/// </summary>
[ApiController]
[Route("v1/billing")]
public class BillingController : ControllerBase
{
    private static readonly HashSet<string> ActiveStatuses = new() { "active", "past_due", "trial" };

    private readonly AppDbContext _db;
    private readonly ICulqiService _culqi;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        AppDbContext db,
        ICulqiService culqi,
        IEventPublisher eventPublisher,
        ILogger<BillingController> logger)
    {
        _db = db;
        _culqi = culqi;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    private Guid GetTenantId() => Guid.Parse(User.FindFirstValue("tenant_id")!);

    [HttpGet("subscription")]
    [Authorize]
    public async Task<ActionResult<SubscriptionResponse?>> GetSubscription(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var sub = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId && ActiveStatuses.Contains(s.Status))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return sub is null ? Ok(null) : Ok(MapToResponse(sub));
    }

    [HttpPost("subscribe")]
    [Authorize]
    public async Task<ActionResult<SubscriptionResponse>> Subscribe(
        [FromBody] SubscribeRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { error = "Falta el token de tarjeta." });

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive, ct);
        if (plan is null) return BadRequest(new { error = "Plan no encontrado." });
        if (plan.PriceMonthly <= 0)
            return BadRequest(new { error = "El plan Free no requiere suscripción de pago." });

        var existing = await _db.Subscriptions
            .Where(s => s.TenantId == tenantId && ActiveStatuses.Contains(s.Status))
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
            return Conflict(new { error = "Ya tenés una suscripción activa. Cancelala antes de cambiar de plan." });

        var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, ct);

        try
        {
            var customerId = await _culqi.CreateCustomerAsync(
                request.Email, request.FirstName, request.LastName,
                request.PhoneNumber, request.CountryCode,
                tenant.Direccion, tenant.Distrito, ct);

            var cardId = await _culqi.CreateCardAsync(customerId, request.Token, ct);
            var culqiPlanId = await _culqi.EnsurePlanAsync(plan.Name, plan.PriceMonthly, ct);
            var subscriptionId = await _culqi.CreateSubscriptionAsync(
                cardId, culqiPlanId,
                new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId.ToString(),
                    ["plan_id"] = plan.Id.ToString(),
                }, ct);

            var sub = new Subscription
            {
                TenantId = tenantId,
                PlanId = plan.Id,
                Status = "active",
                StartDate = DateTimeOffset.UtcNow,
                NextBillingDate = DateTimeOffset.UtcNow.AddMonths(1),
                MonthlyAmount = plan.PriceMonthly,
                DocumentsLimit = plan.MaxDocumentsPerMonth,
                DocumentsUsedThisMonth = 0,
                CulqiCustomerId = customerId,
                CulqiCardId = cardId,
                CulqiSubscriptionId = subscriptionId,
            };
            await _db.Subscriptions.AddAsync(sub, ct);

            tenant.PlanId = plan.Id;

            await _db.SaveChangesAsync(ct);

            await TryPublishAsync("subscription.created", sub, plan.Name, ct);

            // Reload with Plan navigation for response.
            sub.Plan = plan;
            return CreatedAtAction(nameof(GetSubscription), null, MapToResponse(sub));
        }
        catch (CulqiApiException ex)
        {
            _logger.LogWarning(ex,
                "Culqi subscribe failed for tenant {TenantId} plan {PlanId}: {Message}",
                tenantId, plan.Id, ex.Message);
            return StatusCode(502, new { error = "El procesador de pago rechazó la operación.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Idempotently push all active paid plans to Culqi as recurrent plans. Safe to re-run —
    /// EnsurePlanAsync no-ops when Plan.CulqiPlanId is already set. Backoffice superadmin only.
    /// </summary>
    [HttpPost("~/v1/admin/billing/sync-plans")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> SyncPlans(CancellationToken ct)
    {
        if (User.FindFirstValue("platform_user") != "true")
            return Forbid();

        var plans = await _db.Plans
            .Where(p => p.IsActive && p.PriceMonthly > 0m)
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync(ct);

        var results = new List<object>();
        foreach (var plan in plans)
        {
            var alreadySynced = !string.IsNullOrEmpty(plan.CulqiPlanId);
            try
            {
                var culqiPlanId = await _culqi.EnsurePlanAsync(plan.Name, plan.PriceMonthly, ct);
                results.Add(new
                {
                    plan = plan.Name,
                    priceMonthly = plan.PriceMonthly,
                    culqiPlanId,
                    status = alreadySynced ? "already_synced" : "created",
                });
            }
            catch (CulqiApiException ex)
            {
                _logger.LogWarning(ex, "Culqi EnsurePlan failed for {Plan}", plan.Name);
                results.Add(new
                {
                    plan = plan.Name,
                    priceMonthly = plan.PriceMonthly,
                    culqiPlanId = (string?)null,
                    status = "error",
                    error = ex.Message,
                });
            }
        }

        return Ok(new { count = plans.Count, plans = results });
    }

    [HttpPost("cancel")]
    [Authorize]
    public async Task<ActionResult> Cancel([FromBody] CancelSubscriptionRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var sub = await _db.Subscriptions
            .Where(s => s.TenantId == tenantId && ActiveStatuses.Contains(s.Status))
            .FirstOrDefaultAsync(ct);
        if (sub is null) return NotFound(new { error = "No tenés suscripción activa para cancelar." });

        if (!string.IsNullOrEmpty(sub.CulqiSubscriptionId))
        {
            try { await _culqi.CancelSubscriptionAsync(sub.CulqiSubscriptionId, ct); }
            catch (CulqiApiException ex)
            {
                // 404 from Culqi = already cancelled remotely; keep going to mark local state.
                if (ex.StatusCode != 404)
                {
                    _logger.LogWarning(ex, "Culqi cancel failed for subscription {SubId}", sub.CulqiSubscriptionId);
                    return StatusCode(502, new { error = "No se pudo cancelar en Culqi. Reintenta en unos minutos." });
                }
            }
        }

        sub.Status = "cancelled";
        sub.EndDate = DateTimeOffset.UtcNow;
        sub.CancellationReason = request.Reason?.Trim();
        sub.UpdatedAt = DateTimeOffset.UtcNow;

        var freePlan = await _db.Plans
            .Where(p => p.IsActive && p.PriceMonthly == 0m)
            .OrderBy(p => p.PriceMonthly).FirstOrDefaultAsync(ct);
        if (freePlan is not null)
        {
            var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, ct);
            tenant.PlanId = freePlan.Id;
        }

        await _db.SaveChangesAsync(ct);
        await TryPublishAsync("subscription.cancelled", sub, sub.Plan?.Name ?? "?", ct);

        return NoContent();
    }

    /// <summary>
    /// Culqi webhook receiver. Signature MUST be verified — anyone can POST here.
    /// Body is read raw to preserve byte ordering for HMAC.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        var rawBody = ms.ToArray();

        var signature = Request.Headers["culqi-signature"].ToString();
        if (string.IsNullOrEmpty(signature))
            signature = Request.Headers["x-culqi-webhook-signature"].ToString();

        if (!_culqi.VerifyWebhookSignature(rawBody, signature))
        {
            _logger.LogWarning("Culqi webhook rejected: bad signature (got {Sig})", signature);
            return Unauthorized();
        }

        JsonDocument doc;
        try { doc = JsonDocument.Parse(rawBody); }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Culqi webhook body is not JSON");
            return BadRequest();
        }

        try
        {
            var root = doc.RootElement;
            var eventType = root.TryGetProperty("type", out var t) ? t.GetString() : null
                            ?? (root.TryGetProperty("object", out var o) ? o.GetString() : null);
            var data = root.TryGetProperty("data", out var d) ? d : root;

            switch (eventType)
            {
                case "charge.succeeded":
                case "charge.creation.succeeded":
                    await HandleChargeSucceededAsync(data, ct);
                    break;
                case "subscription.cancelled":
                case "subscription.canceled":
                    await HandleSubscriptionCancelledAsync(data, ct);
                    break;
                case "subscription.past_due":
                case "charge.failed":
                    await HandleSubscriptionPastDueAsync(data, ct);
                    break;
                default:
                    _logger.LogInformation("Culqi webhook unhandled type: {Type}", eventType);
                    break;
            }
            return Ok();
        }
        finally { doc.Dispose(); }
    }

    private async Task HandleChargeSucceededAsync(JsonElement data, CancellationToken ct)
    {
        var culqiSubId = data.TryGetProperty("subscription_id", out var s) ? s.GetString() : null;
        var chargeId = data.TryGetProperty("id", out var c) ? c.GetString() : null;
        if (string.IsNullOrEmpty(culqiSubId)) return;

        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(x => x.CulqiSubscriptionId == culqiSubId, ct);
        if (sub is null) return;

        sub.Status = "active";
        sub.LastChargeId = chargeId;
        sub.LastChargedAt = DateTimeOffset.UtcNow;
        sub.NextBillingDate = DateTimeOffset.UtcNow.AddMonths(1);
        sub.DocumentsUsedThisMonth = 0;
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleSubscriptionPastDueAsync(JsonElement data, CancellationToken ct)
    {
        var culqiSubId = data.TryGetProperty("subscription_id", out var s) ? s.GetString()
                        : data.TryGetProperty("id", out var i) ? i.GetString() : null;
        if (string.IsNullOrEmpty(culqiSubId)) return;

        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(x => x.CulqiSubscriptionId == culqiSubId, ct);
        if (sub is null) return;

        sub.Status = "past_due";
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleSubscriptionCancelledAsync(JsonElement data, CancellationToken ct)
    {
        var culqiSubId = data.TryGetProperty("id", out var i) ? i.GetString() : null;
        if (string.IsNullOrEmpty(culqiSubId)) return;

        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(x => x.CulqiSubscriptionId == culqiSubId, ct);
        if (sub is null) return;

        sub.Status = "cancelled";
        sub.EndDate = DateTimeOffset.UtcNow;
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task TryPublishAsync(string subject, Subscription sub, string planName, CancellationToken ct)
    {
        try
        {
            await _eventPublisher.PublishAsync(subject, new
            {
                sub.Id,
                sub.TenantId,
                sub.PlanId,
                planName,
                sub.Status,
                sub.MonthlyAmount,
                sub.CulqiSubscriptionId,
                Timestamp = DateTimeOffset.UtcNow,
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Publish {Subject} failed for sub {Id}", subject, sub.Id);
        }
    }

    private static SubscriptionResponse MapToResponse(Subscription s) => new(
        s.Id, s.TenantId, s.PlanId, s.Plan?.Name ?? "?", s.Status, s.MonthlyAmount,
        s.DocumentsLimit, s.DocumentsUsedThisMonth, s.StartDate, s.EndDate,
        s.NextBillingDate, s.LastChargedAt, s.LastChargeId,
        IsCulqiManaged: !string.IsNullOrEmpty(s.CulqiSubscriptionId));
}
