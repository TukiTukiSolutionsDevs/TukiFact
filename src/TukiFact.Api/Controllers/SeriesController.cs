using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Series;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/series")]
[Authorize]
public class SeriesController : ControllerBase
{
    private readonly ISeriesRepository _seriesRepo;
    private readonly ITenantProvider _tenantProvider;

    public SeriesController(ISeriesRepository seriesRepo, ITenantProvider tenantProvider)
    {
        _seriesRepo = seriesRepo;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var series = await _seriesRepo.GetByTenantAsync(tenantId, ct);
        var response = series.Select(s => new SeriesResponse(
            s.Id, s.DocumentType, s.Serie, s.CurrentCorrelative, s.EmissionPoint, s.IsActive, s.CreatedAt));
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateSeriesRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Check for duplicate
        var existing = await _seriesRepo.GetByTypeAndSerieAsync(tenantId, request.DocumentType, request.Serie, ct);
        if (existing is not null)
            return Conflict(new { error = $"La serie {request.Serie} ya existe para tipo {request.DocumentType}" });

        var series = new Series
        {
            TenantId = tenantId,
            DocumentType = request.DocumentType,
            Serie = request.Serie,
            EmissionPoint = request.EmissionPoint
        };

        await _seriesRepo.CreateAsync(series, ct);
        var response = new SeriesResponse(
            series.Id, series.DocumentType, series.Serie, series.CurrentCorrelative, series.EmissionPoint, series.IsActive, series.CreatedAt);
        return Created($"/v1/series/{series.Id}", response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSeriesRequest request, CancellationToken ct)
    {
        var series = await _seriesRepo.GetByIdAsync(id, ct);
        if (series is null) return NotFound();

        if (request.IsActive.HasValue) series.IsActive = request.IsActive.Value;
        if (request.EmissionPoint is not null) series.EmissionPoint = request.EmissionPoint;

        await _seriesRepo.UpdateAsync(series, ct);
        return NoContent();
    }
}

public record UpdateSeriesRequest(bool? IsActive, string? EmissionPoint);
