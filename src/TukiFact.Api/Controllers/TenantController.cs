using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/tenant")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantRepository _tenantRepo;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ITenantRepository tenantRepo, ITenantProvider tenantProvider, ILogger<TenantController> logger)
    {
        _tenantRepo = tenantRepo;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get current tenant info (company data)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTenantInfo(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        if (tenant is null) return NotFound();

        return Ok(new
        {
            tenant.Id,
            tenant.Ruc,
            tenant.RazonSocial,
            tenant.NombreComercial,
            tenant.Direccion,
            tenant.Ubigeo,
            tenant.Departamento,
            tenant.Provincia,
            tenant.Distrito,
            tenant.LogoUrl,
            tenant.PrimaryColor,
            tenant.Environment,
            tenant.IsActive,
            PlanName = tenant.Plan?.Name ?? "Free",
            PlanMaxDocs = tenant.Plan?.MaxDocumentsPerMonth ?? 50,
            HasCertificate = tenant.CertificateData is not null,
            CertificateExpiresAt = tenant.CertificateExpiresAt,
            HasSunatCredentials = tenant.SunatUser is not null,
            tenant.CreatedAt,
        });
    }

    /// <summary>
    /// Update tenant company data
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateTenant([FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        if (tenant is null) return NotFound();

        if (request.NombreComercial is not null) tenant.NombreComercial = request.NombreComercial;
        if (request.Direccion is not null) tenant.Direccion = request.Direccion;
        if (request.Ubigeo is not null) tenant.Ubigeo = request.Ubigeo;
        if (request.Departamento is not null) tenant.Departamento = request.Departamento;
        if (request.Provincia is not null) tenant.Provincia = request.Provincia;
        if (request.Distrito is not null) tenant.Distrito = request.Distrito;
        if (request.PrimaryColor is not null) tenant.PrimaryColor = request.PrimaryColor;
        if (request.SunatUser is not null) tenant.SunatUser = request.SunatUser;

        await _tenantRepo.UpdateAsync(tenant, ct);
        _logger.LogInformation("Tenant {TenantId} updated", tenantId);
        return NoContent();
    }

    /// <summary>
    /// Upload digital certificate (.pfx or .pem)
    /// </summary>
    [HttpPost("certificate")]
    [Authorize(Roles = "admin")]
    [RequestSizeLimit(5_000_000)] // 5MB max
    public async Task<IActionResult> UploadCertificate(
        IFormFile file,
        [FromForm] string? password,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo de certificado requerido" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".pfx" and not ".p12" and not ".pem")
            return BadRequest(new { error = "Formato inválido. Acepta: .pfx, .p12, .pem" });

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        if (tenant is null) return NotFound();

        // Read certificate data
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var certData = ms.ToArray();

        // Validate certificate can be loaded
        try
        {
            System.Security.Cryptography.X509Certificates.X509Certificate2 cert;
            var certPassword = password ?? string.Empty;

            if (ext == ".pem")
            {
                // PEM: text format with cert + private key
                var pemText = System.Text.Encoding.UTF8.GetString(certData);
                cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(
                    pemText, pemText);
                // Store PEM as-is. The signing service handles both PEM and PFX.
            }
            else
            {
                // PFX/P12: binary PKCS12 format
                cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12(certData, certPassword);
            }

            // Store the original cert data (PEM text or PFX binary)
            tenant.CertificateData = certData;
            tenant.CertificatePasswordEncrypted = ext == ".pem" ? $"PEM:{certPassword}" : certPassword;
            // Npgsql requires UTC offset — cert.NotAfter is local timezone
            tenant.CertificateExpiresAt = new DateTimeOffset(cert.NotAfter.ToUniversalTime(), TimeSpan.Zero);
            await _tenantRepo.UpdateAsync(tenant, ct);

            _logger.LogInformation("Certificate uploaded for tenant {TenantId}, format {Format}, expires {Expiry}",
                tenantId, ext, cert.NotAfter);

            return Ok(new
            {
                message = "Certificado cargado exitosamente",
                subject = cert.Subject,
                issuer = cert.Issuer,
                expiresAt = cert.NotAfter,
                validFrom = cert.NotBefore,
                format = ext.ToUpperInvariant().TrimStart('.'),
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid certificate uploaded for tenant {TenantId}", tenantId);
            return BadRequest(new { error = $"Certificado inválido: {ex.Message}. Verifica el archivo y la contraseña." });
        }
    }

    /// <summary>
    /// Remove digital certificate
    /// </summary>
    [HttpDelete("certificate")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RemoveCertificate(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        if (tenant is null) return NotFound();

        tenant.CertificateData = null;
        tenant.CertificatePasswordEncrypted = null;
        tenant.CertificateExpiresAt = null;
        await _tenantRepo.UpdateAsync(tenant, ct);

        return NoContent();
    }

    /// <summary>
    /// Switch SUNAT environment (beta/production)
    /// </summary>
    [HttpPut("environment")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SetEnvironment([FromBody] SetEnvironmentRequest request, CancellationToken ct)
    {
        if (request.Environment is not "beta" and not "production")
            return BadRequest(new { error = "Entorno inválido. Válidos: beta, production" });

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        if (tenant is null) return NotFound();

        tenant.Environment = request.Environment;
        await _tenantRepo.UpdateAsync(tenant, ct);

        return Ok(new { environment = tenant.Environment });
    }
}

public record UpdateTenantRequest(
    string? NombreComercial, string? Direccion, string? Ubigeo,
    string? Departamento, string? Provincia, string? Distrito,
    string? PrimaryColor, string? SunatUser
);

public record SetEnvironmentRequest(string Environment);
