using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

/// <summary>
/// Sandbox management for developer testing.
/// Creates temporary tenants with sandbox API keys for API testing.
/// </summary>
[ApiController]
[Route("v1/sandbox")]
public class SandboxController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<SandboxController> _logger;

    public SandboxController(AppDbContext db, IPasswordHasher passwordHasher, ILogger<SandboxController> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Create a sandbox environment for developer testing.
    /// Returns API key, credentials, and tenant info.
    /// Sandbox tenants auto-expire after 30 days.
    /// SUNAT always in beta mode. Documents have SANDBOX watermark.
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateSandbox([FromBody] CreateSandboxRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email requerido" });

        // Check if sandbox already exists for this email
        var existingUser = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Tenant.Ruc.StartsWith("SANDBOX-"), ct);

        if (existingUser is not null)
        {
            return Ok(new
            {
                message = "Sandbox ya existe para este email",
                tenantId = existingUser.TenantId,
                email = existingUser.Email,
                note = "Usa las credenciales que recibiste al crear el sandbox"
            });
        }

        // Get free plan
        var freePlan = await _db.Plans.FirstOrDefaultAsync(p => p.Name == "Free", ct);

        // Create sandbox tenant
        var tenant = new TukiFact.Domain.Entities.Tenant
        {
            Ruc = $"SANDBOX-{Guid.NewGuid():N}"[..20],
            RazonSocial = request.CompanyName ?? $"Sandbox de {request.Email}",
            NombreComercial = "SANDBOX",
            Environment = "beta", // Always beta — never send to real SUNAT
            IsActive = true,
            PlanId = freePlan?.Id,
        };
        await _db.Tenants.AddAsync(tenant, ct);

        // Create sandbox user
        var password = GenerateRandomPassword();
        var user = new TukiFact.Domain.Entities.User
        {
            TenantId = tenant.Id,
            Email = request.Email,
            FullName = "Sandbox Developer",
            Role = "admin",
            IsActive = true,
            PasswordHash = _passwordHasher.Hash(password)
        };
        await _db.Users.AddAsync(user, ct);

        // Create sandbox API key
        var rawKey = $"tk_sandbox_{Guid.NewGuid():N}";
        var sandboxExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var apiKey = new TukiFact.Domain.Entities.ApiKey
        {
            TenantId = tenant.Id,
            Name = "Sandbox API Key (auto-expires in 30d)",
            KeyPrefix = rawKey[..14],
            KeyHash = ComputeSha256(rawKey),
            IsActive = true
        };
        await _db.ApiKeys.AddAsync(apiKey, ct);

        // Create default series
        var series = new[]
        {
            new TukiFact.Domain.Entities.Series { TenantId = tenant.Id, DocumentType = "01", Serie = "F001", IsActive = true },
            new TukiFact.Domain.Entities.Series { TenantId = tenant.Id, DocumentType = "03", Serie = "B001", IsActive = true },
            new TukiFact.Domain.Entities.Series { TenantId = tenant.Id, DocumentType = "07", Serie = "FC01", IsActive = true },
            new TukiFact.Domain.Entities.Series { TenantId = tenant.Id, DocumentType = "08", Serie = "FD01", IsActive = true },
        };
        await _db.Series.AddRangeAsync(series, ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Sandbox created for {Email}, tenant: {TenantId}", request.Email, tenant.Id);

        return Created($"/v1/sandbox/{tenant.Id}", new
        {
            tenantId = tenant.Id,
            apiKey = rawKey,
            credentials = new
            {
                email = request.Email,
                password
            },
            expiresAt = sandboxExpiresAt,
            limits = new
            {
                documents = 100,
                requestsPerHour = 200
            },
            environment = "beta",
            note = "Este es un sandbox de prueba. Los documentos NO se envian a SUNAT real. PDFs tienen marca SANDBOX.",
            quickstart = new
            {
                step1 = "npm install @tukifact/sdk",
                step2 = $"const tuki = new TukiFact({{ apiKey: '{rawKey[..20]}...' }})",
                step3 = "await tuki.documents.emit({ documentType: '01', ... })"
            }
        });
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
        var random = new Random();
        return new string(Enumerable.Range(0, 12).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public record CreateSandboxRequest(string Email, string? CompanyName);
}
