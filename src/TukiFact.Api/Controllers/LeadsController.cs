using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Leads;
using TukiFact.Application.Interfaces;
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

    private const string TurnstileVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly AppDbContext _db;
    private readonly IEventPublisher _eventPublisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LeadsController> _logger;

    public LeadsController(
        AppDbContext db,
        IEventPublisher eventPublisher,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LeadsController> logger)
    {
        _db = db;
        _eventPublisher = eventPublisher;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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

        var turnstileSecret = _configuration["Turnstile:SecretKey"];
        if (!string.IsNullOrWhiteSpace(turnstileSecret))
        {
            var (ok, errorCodes) = await VerifyTurnstileAsync(turnstileSecret, request.TurnstileToken, ct);
            if (!ok)
            {
                _logger.LogWarning("Lead rejected: Turnstile verify failed for {Email} (codes: {Codes})",
                    email, string.Join(",", errorCodes));
                return BadRequest(new { error = "No pudimos verificar que seas humano. Reintenta el desafío y vuelve a enviar." });
            }
        }

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

        // Loud INFO log so ops sees it in tails; future LeadNotificationHandler can email/Slack the team.
        _logger.LogInformation("LEAD RECEIVED: {Name} <{Email}> reason={Reason} company={Company}",
            lead.Name, lead.Email, lead.Reason, lead.Company ?? "-");

        try
        {
            await _eventPublisher.PublishAsync("lead.created", new LeadCreatedEvent(
                lead.Id, lead.Name, lead.Email, lead.Company, lead.Phone,
                lead.Reason, lead.Message, lead.Source, lead.CreatedAt), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Publish lead.created failed for {Email} (lead {Id} persisted OK)", lead.Email, lead.Id);
        }

        return CreatedAtAction(nameof(Create), new { id = lead.Id }, new LeadResponse(
            lead.Id, lead.Name, lead.Email, lead.Company, lead.Reason, lead.Status, lead.CreatedAt));
    }

    public record LeadCreatedEvent(
        Guid LeadId,
        string Name,
        string Email,
        string? Company,
        string? Phone,
        string Reason,
        string Message,
        string Source,
        DateTimeOffset CreatedAt);

    private async Task<(bool ok, IReadOnlyList<string> errorCodes)> VerifyTurnstileAsync(
        string secret, string? token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, new[] { "missing-input-response" });

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var form = new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = token,
            };
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp))
                form["remoteip"] = remoteIp;

            using var content = new FormUrlEncodedContent(form);
            using var response = await client.PostAsync(TurnstileVerifyUrl, content, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var success = doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
            var codes = new List<string>();
            if (doc.RootElement.TryGetProperty("error-codes", out var errs) && errs.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in errs.EnumerateArray())
                {
                    var c = e.GetString();
                    if (!string.IsNullOrEmpty(c)) codes.Add(c);
                }
            }
            return (success, codes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Turnstile verify call failed");
            return (false, new[] { "internal-error" });
        }
    }
}
