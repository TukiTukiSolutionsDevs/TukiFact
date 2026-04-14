using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/sire")]
[Authorize]
public class SireController : ControllerBase
{
    private readonly ISireClient _sireClient;
    private readonly ITenantRepository _tenantRepo;
    private readonly ILogger<SireController> _logger;

    public SireController(ISireClient sireClient, ITenantRepository tenantRepo, ILogger<SireController> logger)
    {
        _sireClient = sireClient;
        _tenantRepo = tenantRepo;
        _logger = logger;
    }

    private Guid GetTenantId() => Guid.Parse(User.FindFirstValue("tenant_id")!);

    /// <summary>Get SIRE proposal for a period (YYYYMM)</summary>
    [HttpGet("proposal/{period}")]
    public async Task<IActionResult> GetProposal(string period, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(GetTenantId(), ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        // TODO: Get SOL credentials from TenantServiceConfig
        var token = await _sireClient.GetTokenAsync(
            tenant.Ruc, "MODDATOS", "moddatos",
            tenant.GreClientId ?? "", tenant.GreClientSecret ?? "", ct);

        var proposal = await _sireClient.GetProposalAsync(token, tenant.Ruc, period, ct);
        return Ok(proposal);
    }

    /// <summary>Accept SUNAT RVIE proposal as-is</summary>
    [HttpPost("proposal/{period}/accept")]
    public async Task<IActionResult> AcceptProposal(string period, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(GetTenantId(), ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        var token = await _sireClient.GetTokenAsync(
            tenant.Ruc, "MODDATOS", "moddatos",
            tenant.GreClientId ?? "", tenant.GreClientSecret ?? "", ct);

        var result = await _sireClient.AcceptProposalAsync(token, tenant.Ruc, period, ct);
        return Ok(result);
    }

    /// <summary>Upload replacement RVIE file</summary>
    [HttpPost("proposal/{period}/replace")]
    public async Task<IActionResult> UploadReplacement(string period, IFormFile file, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(GetTenantId(), ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        var token = await _sireClient.GetTokenAsync(
            tenant.Ruc, "MODDATOS", "moddatos",
            tenant.GreClientId ?? "", tenant.GreClientSecret ?? "", ct);

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var result = await _sireClient.UploadReplacementAsync(token, tenant.Ruc, period, ms.ToArray(), ct);
        return Ok(result);
    }

    /// <summary>Check ticket status</summary>
    [HttpGet("ticket/{ticket}/status")]
    public async Task<IActionResult> GetTicketStatus(string ticket, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(GetTenantId(), ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        var token = await _sireClient.GetTokenAsync(
            tenant.Ruc, "MODDATOS", "moddatos",
            tenant.GreClientId ?? "", tenant.GreClientSecret ?? "", ct);

        var status = await _sireClient.GetTicketStatusAsync(token, tenant.Ruc, ticket, ct);
        return Ok(status);
    }

    /// <summary>Download SIRE report (PDF or Excel)</summary>
    [HttpGet("report/{period}")]
    public async Task<IActionResult> DownloadReport(string period, [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        var tenant = await _tenantRepo.GetByIdAsync(GetTenantId(), ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        var token = await _sireClient.GetTokenAsync(
            tenant.Ruc, "MODDATOS", "moddatos",
            tenant.GreClientId ?? "", tenant.GreClientSecret ?? "", ct);

        var report = await _sireClient.DownloadReportAsync(token, tenant.Ruc, period, format, ct);

        var contentType = format.ToLower() == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var extension = format.ToLower() == "pdf" ? "pdf" : "xlsx";

        return File(report, contentType, $"SIRE-{tenant.Ruc}-{period}.{extension}");
    }
}
