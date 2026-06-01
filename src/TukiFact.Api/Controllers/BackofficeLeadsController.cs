using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

/// <summary>
/// Backoffice CRUD over inbound leads (public POST lives in LeadsController.cs).
/// Auth: platform staff only (PlatformUser JWT with superadmin/support/ops role).
/// </summary>
[ApiController]
[Route("v1/backoffice/leads")]
[Authorize(Roles = "superadmin,support,ops")]
public class BackofficeLeadsController : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "new", "contacted", "qualified", "dropped",
    };

    private readonly AppDbContext _db;
    private readonly ILogger<BackofficeLeadsController> _logger;

    public BackofficeLeadsController(AppDbContext db, ILogger<BackofficeLeadsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Leads.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            if (AllowedStatuses.Contains(s))
                query = query.Where(l => l.Status == s);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILIKE via EF.Functions for case-insensitive search on email/name/company.
            var like = $"%{search.Trim()}%";
            query = query.Where(l =>
                EF.Functions.ILike(l.Email, like) ||
                EF.Functions.ILike(l.Name, like) ||
                (l.Company != null && EF.Functions.ILike(l.Company, like)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadAdminResponse(
                l.Id, l.Name, l.Email, l.Company, l.Phone, l.Reason, l.Message,
                l.Source, l.Status, l.Notes, l.CreatedAt, l.ContactedAt))
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var l = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l is null) return NotFound();
        return Ok(new LeadAdminResponse(
            l.Id, l.Name, l.Email, l.Company, l.Phone, l.Reason, l.Message,
            l.Source, l.Status, l.Notes, l.CreatedAt, l.ContactedAt));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadRequest request, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var newStatus = request.Status.Trim().ToLowerInvariant();
            if (!AllowedStatuses.Contains(newStatus))
                return BadRequest(new { error = $"Status inválido. Permitidos: {string.Join(", ", AllowedStatuses)}" });

            if (lead.Status != newStatus)
            {
                lead.Status = newStatus;
                // First transition out of "new" stamps the contact time so the sales funnel has a baseline.
                if (newStatus is "contacted" or "qualified" && lead.ContactedAt is null)
                    lead.ContactedAt = DateTimeOffset.UtcNow;
            }
        }

        if (request.Notes is not null)
        {
            var notes = request.Notes.Trim();
            lead.Notes = notes.Length == 0 ? null : notes;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Lead {Id} updated to status={Status} by backoffice", lead.Id, lead.Status);

        return Ok(new LeadAdminResponse(
            lead.Id, lead.Name, lead.Email, lead.Company, lead.Phone, lead.Reason, lead.Message,
            lead.Source, lead.Status, lead.Notes, lead.CreatedAt, lead.ContactedAt));
    }

    public record UpdateLeadRequest(string? Status, string? Notes);

    public record LeadAdminResponse(
        Guid Id,
        string Name,
        string Email,
        string? Company,
        string? Phone,
        string Reason,
        string Message,
        string Source,
        string Status,
        string? Notes,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ContactedAt);
}
