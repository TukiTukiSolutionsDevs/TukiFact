using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Leads;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

/// <summary>
/// Public endpoint for inbound leads from the marketing site (no auth).
/// </summary>
[ApiController]
[Route("v1/leads")]
[AllowAnonymous]
public class LeadsController : ControllerBase
{
    private static readonly Regex EmailRx = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "ventas", "integracion", "soporte", "general",
    };

    private readonly AppDbContext _db;
    private readonly ILogger<LeadsController> _logger;

    public LeadsController(AppDbContext db, ILogger<LeadsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<LeadResponse>> Create([FromBody] CreateLeadRequest request, CancellationToken ct)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var message = (request.Message ?? string.Empty).Trim();
        var reason = (request.Reason ?? "general").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 150)
            return BadRequest(new { error = "El nombre es requerido (2 a 150 caracteres)." });
        if (string.IsNullOrWhiteSpace(email) || email.Length > 255 || !EmailRx.IsMatch(email))
            return BadRequest(new { error = "El email no es válido." });
        if (string.IsNullOrWhiteSpace(message) || message.Length < 5 || message.Length > 4000)
            return BadRequest(new { error = "El mensaje es requerido (5 a 4000 caracteres)." });
        if (!AllowedReasons.Contains(reason))
            reason = "general";

        var lead = new Lead
        {
            Name = name,
            Email = email,
            Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Reason = reason,
            Message = message,
            Source = "website",
            Status = "new",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
        };

        await _db.Leads.AddAsync(lead, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Lead received from {Email} reason={Reason}", email, reason);

        return CreatedAtAction(nameof(Create), new { id = lead.Id }, new LeadResponse(
            lead.Id, lead.Name, lead.Email, lead.Company, lead.Reason, lead.Status, lead.CreatedAt));
    }
}
