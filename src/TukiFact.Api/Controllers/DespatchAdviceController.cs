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
            var result = await _service.CreateAsync(request, tenantId, ct);

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
            var result = await _service.EmitAsync(id, tenantId, ct);

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
    /// Get a GRE by ID with all its items.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
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
