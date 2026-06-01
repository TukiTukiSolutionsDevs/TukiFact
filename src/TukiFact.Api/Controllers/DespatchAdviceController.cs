using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.DespatchAdvices;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/despatch-advices")]
[Authorize]
public class DespatchAdviceController : ControllerBase
{
    private readonly IDespatchAdviceService _service;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DespatchAdviceController> _logger;

    public DespatchAdviceController(
        IDespatchAdviceService service,
        ITenantProvider tenantProvider,
        ILogger<DespatchAdviceController> logger)
    {
        _service = service;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Create a new DespatchAdvice (GRE) as draft.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> Create([FromBody] CreateDespatchAdviceRequest request, CancellationToken ct)
    {
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _service.CreateAsync(request, tenantId, GetUserId(), ct);

            _logger.LogInformation("GRE created: {FullNumber}", result.FullNumber);
            return Created($"/v1/despatch-advices/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GRE");
            return StatusCode(500, new { error = "Error al crear guía de remisión", detail = ex.Message });
        }
    }

    /// <summary>
    /// Emit (sign + send to SUNAT) an existing draft GRE.
    /// </summary>
    [HttpPost("{id:guid}/emit")]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> Emit(Guid id, CancellationToken ct)
    {
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _service.EmitAsync(id, tenantId, GetUserId(), ct);

            _logger.LogInformation("GRE emitted: {FullNumber} Status: {Status}",
                result.FullNumber, result.Status);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emitting GRE {Id}", id);
            return StatusCode(500, new { error = "Error al emitir guía de remisión", detail = ex.Message });
        }
    }

    /// <summary>
    /// Get a GRE by ID with all its items. Scoped to the caller's tenant.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var result = await _service.GetByIdAsync(id, tenantId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Cancel an accepted GRE locally and record the audit trail. The formal SUNAT
    /// Comunicación de Baja flow still needs to happen via SOL portal.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRequest? request, CancellationToken ct)
    {
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _service.CancelAsync(id, tenantId, GetUserId(), request?.Reason, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling GRE {Id}", id);
            return StatusCode(500, new { error = "Error al anular guía", detail = ex.Message });
        }
    }

    public record CancelRequest(string? Reason);

    /// <summary>
    /// Re-poll SUNAT for a GRE stuck in 'sent' (ticket assigned, no CDR yet).
    /// Use when the inline polling during /emit timed out.
    /// </summary>
    [HttpPost("{id:guid}/refresh-status")]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> RefreshStatus(Guid id, CancellationToken ct)
    {
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _service.RefreshStatusAsync(id, tenantId, GetUserId(), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing GRE status {Id}", id);
            return StatusCode(500, new { error = "Error consultando estado de la guía", detail = ex.Message });
        }
    }

    /// <summary>
    /// List GREs with filters and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? documentType = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var (items, totalCount) = await _service.ListAsync(
            tenantId, page, pageSize, documentType, status, dateFrom, dateTo, ct);

        return Ok(new
        {
            data = items,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }
}
