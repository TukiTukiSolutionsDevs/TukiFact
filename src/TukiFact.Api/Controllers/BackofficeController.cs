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
        await LogPlatformAction("tenant.suspended", "Tenant", tenantId);

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
        await LogPlatformAction("tenant.activated", "Tenant", tenantId);

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
        await LogPlatformAction("tenant.plan_changed", "Tenant", tenantId, System.Text.Json.JsonSerializer.Serialize(new { planId = plan.Id, planName = plan.Name }));

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

    // ==================== M3.1: CRUD EMPLOYEES ====================

    [HttpPost("employees")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        if (await _db.PlatformUsers.AnyAsync(u => u.Email == request.Email, ct))
            return BadRequest(new { error = "Ya existe un empleado con ese email" });

        var validRoles = new[] { "superadmin", "support", "ops", "billing" };
        if (!validRoles.Contains(request.Role))
            return BadRequest(new { error = $"Rol invalido. Roles validos: {string.Join(", ", validRoles)}" });

        var employee = new TukiFact.Domain.Entities.PlatformUser
        {
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role,
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _db.PlatformUsers.AddAsync(employee, ct);
        await _db.SaveChangesAsync(ct);
        await LogPlatformAction("employee.created", "PlatformUser", employee.Id, System.Text.Json.JsonSerializer.Serialize(new { employee.Email, employee.Role }));

        _logger.LogInformation("Platform employee created: {Email} ({Role})", request.Email, request.Role);
        return Created($"/v1/backoffice/employees/{employee.Id}", new { employee.Id, employee.Email, employee.FullName, employee.Role });
    }

    [HttpPut("employees/{id}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var employee = await _db.PlatformUsers.FindAsync([id], ct);
        if (employee is null) return NotFound(new { error = "Empleado no encontrado" });

        if (request.FullName is not null) employee.FullName = request.FullName;
        if (request.Email is not null) employee.Email = request.Email;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Empleado actualizado", employee.Id, employee.Email, employee.FullName, employee.Role });
    }

    [HttpPut("employees/{id}/role")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ChangeEmployeeRole(Guid id, [FromBody] ChangeRoleRequest request, CancellationToken ct)
    {
        var employee = await _db.PlatformUsers.FindAsync([id], ct);
        if (employee is null) return NotFound(new { error = "Empleado no encontrado" });

        var validRoles = new[] { "superadmin", "support", "ops", "billing" };
        if (!validRoles.Contains(request.Role))
            return BadRequest(new { error = "Rol invalido" });

        employee.Role = request.Role;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Employee {Id} role changed to {Role}", id, request.Role);
        return Ok(new { message = $"Rol cambiado a {request.Role}" });
    }

    [HttpDelete("employees/{id}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> DeactivateEmployee(Guid id, CancellationToken ct)
    {
        var employee = await _db.PlatformUsers.FindAsync([id], ct);
        if (employee is null) return NotFound(new { error = "Empleado no encontrado" });

        employee.IsActive = false;
        await _db.SaveChangesAsync(ct);
        await LogPlatformAction("employee.deactivated", "PlatformUser", id);

        _logger.LogWarning("Employee deactivated: {Email}", employee.Email);
        return Ok(new { message = "Empleado desactivado" });
    }

    // ==================== M3.2: IMPERSONATE TENANT ====================

    [HttpPost("tenants/{tenantId}/impersonate")]
    [Authorize(Roles = "superadmin,support")]
    public async Task<IActionResult> ImpersonateTenant(Guid tenantId, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var tenant = await _db.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        var platformUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;

        // Generate short-lived JWT for impersonation (15 min)
        // The JwtService would need to accept impersonation claims
        // For now return the data needed for the frontend to build the impersonation session
        var token = new
        {
            tenantId = tenant.Id,
            tenantName = tenant.RazonSocial,
            tenantRuc = tenant.Ruc,
            impersonatedBy = platformUserId,
            impersonating = true,
            expiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            redirectUrl = $"/dashboard"
        };

        _logger.LogWarning("IMPERSONATION: Platform user {UserId} impersonating tenant {TenantId} ({Ruc})",
            platformUserId, tenantId, tenant.Ruc);

        await LogPlatformAction("tenant.impersonated", "Tenant", tenantId);

        return Ok(token);
    }

    // ==================== M3.4: REPORTS ====================

    [HttpGet("reports/mrr")]
    public async Task<IActionResult> GetMrrReport(CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var activeTenants = await _db.Tenants
            .Where(t => t.IsActive && t.PlanId != null)
            .Include(t => t.Plan)
            .ToListAsync(ct);

        var totalMrr = activeTenants.Sum(t => t.Plan?.PriceMonthly ?? 0);
        var mrrByPlan = activeTenants
            .GroupBy(t => t.Plan?.Name ?? "Sin plan")
            .Select(g => new { plan = g.Key, count = g.Count(), mrr = g.Sum(t => t.Plan?.PriceMonthly ?? 0) })
            .OrderByDescending(x => x.mrr)
            .ToList();

        return Ok(new { totalMrr, mrrByPlan, activeTenantCount = activeTenants.Count });
    }

    [HttpGet("reports/usage")]
    public async Task<IActionResult> GetUsageReport(CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync("SET LOCAL row_security = off;", ct);

        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var todayDocs = await _db.Documents.CountAsync(d => d.CreatedAt >= todayStart, ct);
        var weekDocs = await _db.Documents.CountAsync(d => d.CreatedAt >= weekStart, ct);
        var monthDocs = await _db.Documents.CountAsync(d => d.CreatedAt >= monthStart, ct);

        var topTenants = await _db.Documents
            .Where(d => d.CreatedAt >= monthStart)
            .GroupBy(d => d.TenantId)
            .Select(g => new { tenantId = g.Key, count = g.Count(), total = g.Sum(d => d.Total) })
            .OrderByDescending(x => x.count)
            .Take(10)
            .ToListAsync(ct);

        var byType = await _db.Documents
            .Where(d => d.CreatedAt >= monthStart)
            .GroupBy(d => d.DocumentType)
            .Select(g => new { type = g.Key, count = g.Count() })
            .ToListAsync(ct);

        return Ok(new { todayDocs, weekDocs, monthDocs, topTenants, byType });
    }

    // ==================== M3.6: PLATFORM CONFIG ====================

    [HttpGet("config")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var configs = await _db.PlatformConfigs.ToListAsync(ct);
        var dict = configs.ToDictionary(c => c.Key, c => c.Value);

        string Get(string key, string def) => dict.TryGetValue(key, out var v) ? v : def;

        return Ok(new
        {
            maintenance_mode = Get("maintenance_mode", "false"),
            registration_enabled = Get("registration_enabled", "true"),
            default_plan = Get("default_plan", "free"),
            max_free_documents = Get("max_free_documents", "50"),
            trial_days = Get("trial_days", "14"),
            sunat_beta_mode = Get("sunat_beta_mode", "true"),
            email_provider = Get("email_provider", "resend"),
            support_email = Get("support_email", "soporte@tukifact.net.pe"),
        });
    }

    [HttpPut("config")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> UpdateConfig([FromBody] Dictionary<string, string> settings, CancellationToken ct)
    {
        foreach (var (key, value) in settings)
        {
            var existing = await _db.PlatformConfigs
                .FirstOrDefaultAsync(c => c.Key == key, ct);
            if (existing is not null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                await _db.PlatformConfigs.AddAsync(
                    new TukiFact.Domain.Entities.PlatformConfig { Key = key, Value = value }, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        await LogPlatformAction("config.updated", "PlatformConfig", null, System.Text.Json.JsonSerializer.Serialize(settings));
        _logger.LogInformation("Platform config updated: {Keys}", string.Join(", ", settings.Keys));
        return Ok(new { message = "Configuración actualizada" });
    }

    // ==================== M3.3: ACTIVITY LOG ====================

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivityLog(
        [FromQuery] string? action,
        [FromQuery] Guid? platformUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = _db.PlatformAuditLogs
            .Include(a => a.PlatformUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.Contains(action));
        if (platformUserId.HasValue)
            query = query.Where(a => a.PlatformUserId == platformUserId.Value);
        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id, a.Action, a.EntityType, a.EntityId, a.Details,
                a.IpAddress, a.CreatedAt,
                user = a.PlatformUser != null ? new { a.PlatformUser.Email, a.PlatformUser.FullName, a.PlatformUser.Role } : null
            })
            .ToListAsync(ct);

        return Ok(new
        {
            data = logs,
            pagination = new { page, pageSize, totalCount, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) }
        });
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task LogPlatformAction(string action, string entityType, Guid? entityId, string? details = null)
    {
        var platformUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
        var log = new TukiFact.Domain.Entities.PlatformAuditLog
        {
            PlatformUserId = Guid.TryParse(platformUserId, out var uid) ? uid : null,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
        await _db.PlatformAuditLogs.AddAsync(log);
        await _db.SaveChangesAsync();
    }

    // ==================== DTOs ====================

    public record ChangePlanRequest(Guid PlanId);
    public record CreateEmployeeRequest(string Email, string FullName, string Password, string Role);
    public record UpdateEmployeeRequest(string? Email, string? FullName);
    public record ChangeRoleRequest(string Role);
}
