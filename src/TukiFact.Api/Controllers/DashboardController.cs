using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ITenantProvider _tenantProvider;

    public DashboardController(IDashboardService dashboardService, ITenantProvider tenantProvider)
    {
        _dashboardService = dashboardService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get dashboard summary for the current tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var dashboard = await _dashboardService.GetDashboardAsync(tenantId, ct);
        return Ok(dashboard);
    }
}
