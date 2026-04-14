using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Domain.Interfaces;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/certificate")]
[Authorize(Roles = "admin")]
public class CertificateController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<CertificateController> _logger;

    public CertificateController(AppDbContext db, ITenantProvider tenantProvider, ILogger<CertificateController> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _db.Tenants.FindAsync([tenantId], ct);

        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        return Ok(new
        {
            hasCertificate = tenant.CertificateData != null,
            expiresAt = tenant.CertificateExpiresAt,
            isExpired = tenant.CertificateExpiresAt.HasValue && tenant.CertificateExpiresAt.Value < DateTimeOffset.UtcNow,
            daysUntilExpiry = tenant.CertificateExpiresAt.HasValue
                ? (int)(tenant.CertificateExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays
                : (int?)null,
            environment = tenant.Environment,
            hasSunatCredentials = !string.IsNullOrEmpty(tenant.SunatUser),
        });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB max
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string password,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".pfx" and not ".p12")
            return BadRequest(new { error = "Solo se aceptan archivos .pfx o .p12" });

        // Read certificate bytes
        byte[] certBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, ct);
            certBytes = ms.ToArray();
        }

        // Validate certificate
        DateTimeOffset expiresAt;
        DateTimeOffset issuedAt;
        string subject;
        string serialNumber;
        string thumbprint;
        string issuer;
        bool hasPrivateKey;
        try
        {
            using var cert = X509CertificateLoader.LoadPkcs12(certBytes, password);
            expiresAt = new DateTimeOffset(cert.NotAfter, TimeSpan.Zero);
            issuedAt = new DateTimeOffset(cert.NotBefore, TimeSpan.Zero);
            subject = cert.Subject;
            serialNumber = cert.SerialNumber;
            thumbprint = cert.Thumbprint;
            issuer = cert.Issuer;
            hasPrivateKey = cert.HasPrivateKey;

            if (!hasPrivateKey)
                return BadRequest(new { error = "El certificado no contiene clave privada. Se requiere un .pfx con clave privada." });

            if (expiresAt < DateTimeOffset.UtcNow)
                return BadRequest(new { error = "El certificado ya expiró", expiresAt });

            // Verify RUC is in the subject (M2.2 validation)
            var tenantId2 = _tenantProvider.GetCurrentTenantId();
            var tenant2 = await _db.Tenants.FindAsync([tenantId2], ct);
            if (tenant2 is not null && !string.IsNullOrEmpty(tenant2.Ruc))
            {
                if (!subject.Contains(tenant2.Ruc))
                {
                    _logger.LogWarning("Certificate subject '{Subject}' does not contain RUC {Ruc}",
                        subject, tenant2.Ruc);
                    // Warning, not error — some certificates use different subject formats
                }
            }

            _logger.LogInformation(
                "Certificate validated: {Subject}, serial: {Serial}, expires {ExpiresAt}",
                subject, serialNumber, expiresAt);
        }
        catch (Exception ex) when (ex is not BadHttpRequestException)
        {
            _logger.LogWarning("Certificate validation failed: {Error}", ex.Message);
            return BadRequest(new { error = "No se pudo leer el certificado. Verificá la contraseña.", detail = ex.Message });
        }

        // Save to tenant
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _db.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        tenant.CertificateData = certBytes;
        tenant.CertificatePasswordEncrypted = password; // TODO: encrypt with DataProtection
        tenant.CertificateExpiresAt = expiresAt;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        var daysUntilExpiry = (int)(expiresAt - DateTimeOffset.UtcNow).TotalDays;
        return Ok(new
        {
            message = "Certificado subido correctamente",
            subject,
            issuer,
            serialNumber,
            thumbprint,
            issuedAt,
            expiresAt,
            daysUntilExpiry,
            warning = daysUntilExpiry < 30
                ? $"ATENCION: El certificado expira en {daysUntilExpiry} dias"
                : null,
            hasPrivateKey,
            rucMatch = subject.Contains(tenant.Ruc)
        });
    }

    [HttpDelete]
    public async Task<IActionResult> Remove(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _db.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        tenant.CertificateData = null;
        tenant.CertificatePasswordEncrypted = null;
        tenant.CertificateExpiresAt = null;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Certificado eliminado" });
    }

    [HttpPut("sunat-credentials")]
    public async Task<IActionResult> UpdateSunatCredentials(
        [FromBody] SunatCredentialsRequest request,
        CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _db.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        tenant.SunatUser = request.SunatUser;
        tenant.SunatPasswordEncrypted = request.SunatPassword; // TODO: encrypt
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Credenciales SUNAT actualizadas" });
    }

    [HttpPut("environment")]
    public async Task<IActionResult> ChangeEnvironment(
        [FromBody] ChangeEnvironmentRequest request,
        CancellationToken ct)
    {
        if (request.Environment is not "beta" and not "production")
            return BadRequest(new { error = "Environment debe ser 'beta' o 'production'" });

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _db.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return NotFound(new { error = "Tenant no encontrado" });

        // Validate requirements for production
        if (request.Environment == "production")
        {
            if (tenant.CertificateData is null)
                return BadRequest(new { error = "Se requiere certificado digital para producción" });
            if (string.IsNullOrEmpty(tenant.SunatUser))
                return BadRequest(new { error = "Se requieren credenciales SUNAT para producción" });
        }

        tenant.Environment = request.Environment;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Tenant {TenantId} changed environment to {Env}", tenantId, request.Environment);
        return Ok(new { message = $"Entorno cambiado a {request.Environment}" });
    }

    public record SunatCredentialsRequest(string SunatUser, string SunatPassword);
    public record ChangeEnvironmentRequest(string Environment);
}
