using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/backoffice")]
[Authorize(Roles = "superadmin,support,ops")]
public class BackofficeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<BackofficeController> _logger;

    public BackofficeController(AppDbContext db, ILogger<BackofficeController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== DASHBOARD ====================

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        // Disable RLS for cross-tenant queries
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var totalTenants = await _db.Tenants.CountAsync(ct);
        var activeTenants = await _db.Tenants.CountAsync(t => t.IsActive, ct);
        var totalUsers = await _db.Users.CountAsync(ct);
        var totalDocuments = await _db.Documents.CountAsync(ct);

        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var todayDocs = await _db.Documents
            .CountAsync(d => d.CreatedAt >= todayStart, ct);

        var monthDocs = await _db.Documents
            .CountAsync(d => d.CreatedAt >= monthStart, ct);

        var recentTenants = await _db.Tenants
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new { t.Id, t.Ruc, t.RazonSocial, t.IsActive, t.CreatedAt })
            .ToListAsync(ct);

        var tenantsByPlan = await _db.Tenants
            .Where(t => t.PlanId != null)
            .GroupBy(t => t.Plan!.Name)
            .Select(g => new { plan = g.Key, count = g.Count() })
            .ToListAsync(ct);

        return Ok(new
        {
            totalTenants,
            activeTenants,
            suspendedTenants = totalTenants - activeTenants,
            totalUsers,
            totalDocuments,
            todayDocuments = todayDocs,
            monthDocuments = monthDocs,
            recentTenants,
            tenantsByPlan
        });
    }

    // ==================== TENANTS ====================

    [HttpGet("tenants")]
    public async Task<IActionResult> ListTenants(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var query = _db.Tenants
            .Include(t => t.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                t.Ruc.Contains(search) ||
                t.RazonSocial.Contains(search) ||
                (t.NombreComercial != null && t.NombreComercial.Contains(search)));
        }

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);

        var tenants = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id, t.Ruc, t.RazonSocial, t.NombreComercial,
                t.IsActive, t.Environment, t.CreatedAt,
                plan = t.Plan != null ? t.Plan.Name : "Sin plan",
                usersCount = t.Users.Count,
                documentsCount = t.Documents.Count
            })
            .ToListAsync(ct);

        return Ok(new
        {
            data = tenants,
            pagination = new { page, pageSize, totalCount, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) }
        });
    }

    [HttpGet("tenants/{tenantId}")]
    public async Task<IActionResult> GetTenant(Guid tenantId, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var tenant = await _db.Tenants
            .Include(t => t.Plan)
            .Include(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        var docCount = await _db.Documents.CountAsync(d => d.TenantId == tenantId, ct);
        var tenantMonthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var monthDocCount = await _db.Documents
            .CountAsync(d => d.TenantId == tenantId && d.CreatedAt >= tenantMonthStart, ct);

        return Ok(new
        {
            tenant.Id, tenant.Ruc, tenant.RazonSocial, tenant.NombreComercial,
            tenant.Direccion, tenant.IsActive, tenant.Environment,
            tenant.CreatedAt, tenant.UpdatedAt,
            hasCertificate = tenant.CertificateData != null,
            certificateExpiresAt = tenant.CertificateExpiresAt,
            plan = tenant.Plan != null ? new { tenant.Plan.Id, tenant.Plan.Name, tenant.Plan.PriceMonthly, tenant.Plan.MaxDocumentsPerMonth } : null,
            users = tenant.Users.Select(u => new { u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.LastLoginAt }),
            stats = new { totalDocuments = docCount, monthDocuments = monthDocCount }
        });
    }

    [HttpPut("tenants/{tenantId}/suspend")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> SuspendTenant(Guid tenantId, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var tenant = await _db.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        tenant.IsActive = false;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Tenant suspended: {TenantId} ({Ruc})", tenantId, tenant.Ruc);
        return Ok(new { message = $"Tenant {tenant.RazonSocial} suspendido" });
    }

    [HttpPut("tenants/{tenantId}/activate")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ActivateTenant(Guid tenantId, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var tenant = await _db.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        tenant.IsActive = true;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Tenant activated: {TenantId} ({Ruc})", tenantId, tenant.Ruc);
        return Ok(new { message = $"Tenant {tenant.RazonSocial} activado" });
    }

    [HttpPut("tenants/{tenantId}/plan")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ChangePlan(Guid tenantId, [FromBody] ChangePlanRequest request, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var tenant = await _db.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        var plan = await _db.Plans.FindAsync([request.PlanId], ct);
        if (plan is null) return NotFound(new { error = "Plan no encontrado" });

        tenant.PlanId = plan.Id;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Plan changed for {TenantId}: {PlanName}", tenantId, plan.Name);
        return Ok(new { message = $"Plan cambiado a {plan.Name}" });
    }

    // ==================== SUPPORT: Documents cross-tenant ====================

    [HttpGet("documents")]
    public async Task<IActionResult> SearchDocuments(
        [FromQuery] string? ruc,
        [FromQuery] string? serie,
        [FromQuery] int? correlative,
        [FromQuery] string? customerDocNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var query = _db.Documents
            .Include(d => d.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(ruc))
        {
            var tenantIds = await _db.Tenants
                .Where(t => t.Ruc.Contains(ruc))
                .Select(t => t.Id)
                .ToListAsync(ct);
            query = query.Where(d => tenantIds.Contains(d.TenantId));
        }

        if (!string.IsNullOrWhiteSpace(serie))
            query = query.Where(d => d.Serie == serie);

        if (correlative.HasValue)
            query = query.Where(d => d.Correlative == correlative.Value);

        if (!string.IsNullOrWhiteSpace(customerDocNumber))
            query = query.Where(d => d.CustomerDocNumber == customerDocNumber);

        var totalCount = await query.CountAsync(ct);

        var docs = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id, d.TenantId, d.DocumentType, d.Serie, d.Correlative,
                fullNumber = $"{d.Serie}-{d.Correlative:D8}",
                d.CustomerName, d.CustomerDocNumber,
                d.Total, d.Status, d.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            data = docs,
            pagination = new { page, pageSize, totalCount, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) }
        });
    }

    // ==================== PLATFORM USERS ====================

    [HttpGet("employees")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListEmployees(CancellationToken ct)
    {
        var employees = await _db.PlatformUsers
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new { u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.LastLoginAt, u.CreatedAt })
            .ToListAsync(ct);

        return Ok(employees);
    }

    // ==================== DTOs ====================

    public record ChangePlanRequest(Guid PlanId);
}
