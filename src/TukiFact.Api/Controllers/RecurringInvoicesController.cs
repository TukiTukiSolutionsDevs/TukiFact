using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.RecurringInvoices;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/recurring-invoices")]
[Authorize]
public class RecurringInvoicesController : ControllerBase
{
    private readonly IRecurringInvoiceRepository _recurringRepo;
    private readonly ILogger<RecurringInvoicesController> _logger;

    public RecurringInvoicesController(IRecurringInvoiceRepository recurringRepo, ILogger<RecurringInvoicesController> logger)
    {
        _recurringRepo = recurringRepo;
        _logger = logger;
    }

    private Guid GetTenantId() => Guid.Parse(User.FindFirstValue("tenant_id")!);

    [HttpPost]
    public async Task<ActionResult<RecurringInvoiceResponse>> Create([FromBody] CreateRecurringInvoiceRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();

        var recurring = new RecurringInvoice
        {
            TenantId = tenantId,
            DocumentType = request.DocumentType,
            Serie = request.Serie,
            CustomerDocType = request.CustomerDocType,
            CustomerDocNumber = request.CustomerDocNumber,
            CustomerName = request.CustomerName,
            CustomerAddress = request.CustomerAddress,
            CustomerEmail = request.CustomerEmail,
            Currency = request.Currency ?? "PEN",
            ItemsJson = JsonSerializer.Serialize(request.Items),
            Frequency = request.Frequency,
            DayOfMonth = request.DayOfMonth,
            DayOfWeek = request.DayOfWeek,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            NextEmissionDate = request.StartDate,
            Notes = request.Notes,
            CreatedByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };

        await _recurringRepo.AddAsync(recurring, ct);
        _logger.LogInformation("Recurring invoice created: {Id} — {Frequency} starting {Start}",
            recurring.Id, recurring.Frequency, recurring.StartDate);

        return CreatedAtAction(nameof(GetById), new { id = recurring.Id }, MapToResponse(recurring));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecurringInvoiceResponse>> GetById(Guid id, CancellationToken ct)
    {
        var recurring = await _recurringRepo.GetByIdAsync(id, ct);
        return recurring is null ? NotFound() : Ok(MapToResponse(recurring));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _recurringRepo.ListAsync(GetTenantId(), page, pageSize, status, ct);
        return Ok(new { items = items.Select(MapToResponse), totalCount = total, page, pageSize });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RecurringInvoiceResponse>> Update(Guid id, [FromBody] UpdateRecurringInvoiceRequest request, CancellationToken ct)
    {
        var recurring = await _recurringRepo.GetByIdAsync(id, ct);
        if (recurring is null) return NotFound();
        if (recurring.TenantId != GetTenantId()) return Forbid();

        if (request.Status is not null)
        {
            recurring.Status = request.Status;
            if (request.Status == "cancelled" || request.Status == "paused")
                recurring.NextEmissionDate = null;
            if (request.Status == "active" && recurring.NextEmissionDate is null)
                recurring.NextEmissionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }
        if (request.EndDate.HasValue) recurring.EndDate = request.EndDate;
        if (request.Notes is not null) recurring.Notes = request.Notes;

        await _recurringRepo.UpdateAsync(recurring, ct);
        return Ok(MapToResponse(recurring));
    }

    private static RecurringInvoiceResponse MapToResponse(RecurringInvoice r) => new(
        r.Id, r.DocumentType, r.Serie,
        r.CustomerDocType, r.CustomerDocNumber, r.CustomerName, r.CustomerEmail,
        r.Currency, r.Frequency, r.DayOfMonth, r.DayOfWeek,
        r.StartDate, r.EndDate, r.NextEmissionDate,
        r.Status, r.EmittedCount, r.LastEmittedDate,
        r.Notes, r.CreatedAt);
}
