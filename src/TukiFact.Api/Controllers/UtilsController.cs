using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/utils")]
[Authorize]
public class UtilsController : ControllerBase
{
    private readonly IRucValidationService _rucService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ICpeValidationService _cpeService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<UtilsController> _logger;

    public UtilsController(
        IRucValidationService rucService,
        IExchangeRateService exchangeRateService,
        ICpeValidationService cpeService,
        ITenantProvider tenantProvider,
        ILogger<UtilsController> logger)
    {
        _rucService = rucService;
        _exchangeRateService = exchangeRateService;
        _cpeService = cpeService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Validate a RUC number and return company data.
    /// </summary>
    [HttpGet("validate-ruc/{ruc}")]
    public async Task<IActionResult> ValidateRuc(string ruc, CancellationToken ct)
    {
        if (ruc.Length != 11)
            return BadRequest(new { error = "RUC debe tener 11 dígitos" });

        // TODO: Get apiKey from TenantServiceConfig.LookupApiKey
        var result = await _rucService.ValidateRucAsync(ruc, null, ct);
        return result is null
            ? NotFound(new { error = $"RUC {ruc} no encontrado" })
            : Ok(result);
    }

    /// <summary>
    /// Validate a DNI number and return person data.
    /// </summary>
    [HttpGet("validate-dni/{dni}")]
    public async Task<IActionResult> ValidateDni(string dni, CancellationToken ct)
    {
        if (dni.Length != 8)
            return BadRequest(new { error = "DNI debe tener 8 dígitos" });

        // TODO: Get apiKey from TenantServiceConfig.LookupApiKey
        var result = await _rucService.ValidateDniAsync(dni, null, ct);
        return result is null
            ? NotFound(new { error = $"DNI {dni} no encontrado" })
            : Ok(result);
    }

    /// <summary>
    /// Get exchange rate for a specific date and currency.
    /// Cached daily — only 1 API call per day per currency.
    /// </summary>
    [HttpGet("exchange-rate")]
    public async Task<IActionResult> GetExchangeRate(
        [FromQuery] string? date,
        [FromQuery] string currency = "USD",
        CancellationToken ct = default)
    {
        var targetDate = string.IsNullOrEmpty(date)
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : DateOnly.Parse(date);

        try
        {
            var rate = await _exchangeRateService.GetRateAsync(targetDate, currency, ct);
            return rate is null
                ? NotFound(new { error = $"Tipo de cambio no disponible para {targetDate:yyyy-MM-dd}" })
                : Ok(new
                {
                    rate.Date,
                    rate.Currency,
                    rate.BuyRate,
                    rate.SellRate,
                    rate.Source,
                    rate.FetchedAt
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rate");
            return StatusCode(503, new { error = "No se pudo obtener el tipo de cambio" });
        }
    }

    /// <summary>
    /// Get current plan usage for the authenticated tenant.
    /// Shows documents emitted this month vs plan limit.
    /// </summary>
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage(
        [FromServices] TukiFact.Infrastructure.Persistence.AppDbContext db,
        CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await db.Tenants
            .Include(t => t.Plan)
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
            return NotFound(new { error = "Tenant no encontrado" });

        var firstOfMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var documentsThisMonth = await db.Documents
            .CountAsync(d => d.TenantId == tenantId
                && d.IssueDate >= firstOfMonth
                && d.Status != "draft", ct);

        var limit = tenant.Plan?.MaxDocumentsPerMonth ?? 50;
        var percentage = limit > 0 ? Math.Round((decimal)documentsThisMonth / limit * 100, 1) : 0;

        return Ok(new
        {
            Plan = tenant.Plan?.Name ?? "Free",
            DocumentsThisMonth = documentsThisMonth,
            MaxDocumentsPerMonth = limit,
            UsagePercent = percentage,
            RemainingDocuments = Math.Max(0, limit - documentsThisMonth),
            Warning = percentage >= 80 ? (percentage >= 100 ? "LIMIT_REACHED" : "APPROACHING_LIMIT") : (string?)null
        });
    }

    /// <summary>
    /// Validate a CPE (Comprobante de Pago Electrónico) against SUNAT.
    /// </summary>
    [HttpGet("validate-cpe")]
    public async Task<IActionResult> ValidateCpe(
        [FromQuery] string ruc,
        [FromQuery] string tipo,
        [FromQuery] string serie,
        [FromQuery] string correlativo,
        [FromQuery] string fecha,
        [FromQuery] decimal total,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(ruc) || ruc.Length != 11)
            return BadRequest(new { error = "RUC debe tener 11 dígitos" });

        var fechaEmision = DateOnly.Parse(fecha);
        var result = await _cpeService.ValidateCpeAsync(ruc, tipo, serie, correlativo, fechaEmision, total, ct);

        return result is null
            ? StatusCode(503, new { error = "No se pudo consultar SUNAT" })
            : Ok(result);
    }
}
