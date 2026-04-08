using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.AuditLog;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/audit-log")]
[Authorize(Roles = "admin")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogRepository _auditRepo;
    private readonly ITenantProvider _tenantProvider;

    public AuditLogController(IAuditLogRepository auditRepo, ITenantProvider tenantProvider)
    {
        _auditRepo = auditRepo;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30,
        [FromQuery] string? action = null, [FromQuery] string? entityType = null,
        CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var (items, total) = await _auditRepo.GetByTenantAsync(tenantId, page, pageSize, action, entityType, ct);
        return Ok(new
        {
            data = items.Select(a => new AuditLogResponse(a.Id, a.Action, a.EntityType, a.EntityId, a.Details, a.UserId, a.IpAddress, a.CreatedAt)),
            pagination = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }
}
