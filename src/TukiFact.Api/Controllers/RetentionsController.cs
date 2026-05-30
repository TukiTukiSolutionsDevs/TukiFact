using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Retentions;
using TukiFact.Application.Interfaces;
using TukiFact.Application.Validation;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Enums;
using TukiFact.Domain.Services;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/retentions")]
[Authorize]
public class RetentionsController : ControllerBase
{
    private readonly IRetentionRepository _retentionRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IRetentionXmlBuilder _xmlBuilder;
    private readonly IXmlSigningService _signingService;
    private readonly ISunatClient _sunatClient;
    private readonly IStorageService _storageService;
    private readonly ISecretProtector _secrets;
    private readonly ILogger<RetentionsController> _logger;

    public RetentionsController(
        IRetentionRepository retentionRepo,
        ITenantRepository tenantRepo,
        IRetentionXmlBuilder xmlBuilder,
        IXmlSigningService signingService,
        ISunatClient sunatClient,
        IStorageService storageService,
        ISecretProtector secrets,
        ILogger<RetentionsController> logger)
    {
        _retentionRepo = retentionRepo;
        _tenantRepo = tenantRepo;
        _xmlBuilder = xmlBuilder;
        _signingService = signingService;
        _sunatClient = sunatClient;
        _storageService = storageService;
        _secrets = secrets;
        _logger = logger;
    }

    private Guid GetTenantId() => Guid.Parse(User.FindFirstValue("tenant_id")!);

    /// <summary>Create and emit a retention document (tipo 20)</summary>
    [HttpPost]
    public async Task<ActionResult<RetentionResponse>> Create([FromBody] CreateRetentionRequest request, CancellationToken ct)
    {
        var validationErrors = RetentionValidator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { error = "Datos inválidos para emitir la retención.", details = validationErrors });

        var tenantId = GetTenantId();
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        var correlative = await _retentionRepo.GetNextCorrelativeAsync(tenantId, request.Serie, ct);

        // Build entity
        var retention = new RetentionDocument
        {
            TenantId = tenantId,
            Serie = request.Serie,
            Correlative = correlative,
            IssueDate = request.IssueDate ?? RecurringScheduleCalculator.TodayInLima(),
            SupplierDocType = request.SupplierDocType,
            SupplierDocNumber = request.SupplierDocNumber,
            SupplierName = request.SupplierName,
            SupplierAddress = request.SupplierAddress,
            RegimeCode = request.RegimeCode,
            RetentionPercent = request.RetentionPercent,
            Currency = request.Currency ?? "PEN",
            Notes = request.Notes,
            CreatedByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };

        // Build references and calculate totals
        decimal totalInvoice = 0, totalRetained = 0, totalPaid = 0;

        foreach (var refReq in request.References)
        {
            var retainedAmount = Math.Round(refReq.PaymentAmount * (request.RetentionPercent / 100m), 2);
            var netPaid = refReq.PaymentAmount - retainedAmount;

            retention.References.Add(new RetentionDocumentReference
            {
                DocumentType = refReq.DocumentType,
                DocumentNumber = refReq.DocumentNumber,
                DocumentDate = refReq.DocumentDate,
                InvoiceAmount = refReq.InvoiceAmount,
                InvoiceCurrency = refReq.InvoiceCurrency,
                PaymentDate = refReq.PaymentDate,
                PaymentNumber = refReq.PaymentNumber,
                PaymentAmount = refReq.PaymentAmount,
                RetainedAmount = retainedAmount,
                NetPaidAmount = netPaid,
                ExchangeRate = refReq.ExchangeRate,
                ExchangeRateDate = refReq.ExchangeRateDate
            });

            totalInvoice += refReq.InvoiceAmount;
            totalRetained += retainedAmount;
            totalPaid += netPaid;
        }

        retention.TotalInvoiceAmount = totalInvoice;
        retention.TotalRetained = totalRetained;
        retention.TotalPaid = totalPaid;

        await _retentionRepo.AddAsync(retention, ct);
        _logger.LogInformation("Retention created: {FullNumber}", retention.FullNumber);

        // Build XML (UBL 2.0)
        var xml = _xmlBuilder.BuildRetentionXml(retention, tenant);

        // Sign XML — required; SUNAT rechazará XML sin firma (0306) y dejaría el flujo en estado inconsistente.
        if (tenant.CertificateData is null || tenant.CertificatePasswordEncrypted is null)
        {
            retention.Status = DocumentStatus.Rejected;
            retention.SunatResponseDescription = "El emisor no tiene certificado digital configurado.";
            await _retentionRepo.UpdateAsync(retention, ct);
            return UnprocessableEntity(new { error = "El emisor no tiene certificado digital configurado. Configúralo en /certificate antes de emitir." });
        }

        string signedXml;
        try
        {
            var (signed, digest) = _signingService.SignXml(xml, tenant.CertificateData, _secrets.Unprotect(tenant.CertificatePasswordEncrypted));
            signedXml = signed;
            retention.HashCode = digest;
            retention.Status = DocumentStatus.Signed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign retention {FullNumber}", retention.FullNumber);
            retention.Status = DocumentStatus.Rejected;
            retention.SunatResponseDescription = "Error firmando XML: " + ex.Message;
            await _retentionRepo.UpdateAsync(retention, ct);
            return UnprocessableEntity(new { error = "Error firmando XML", detail = ex.Message });
        }

        // Store XML
        var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
        retention.XmlUrl = await _storageService.UploadXmlAsync(tenantId,
            $"{retention.FullNumber}.xml", xmlBytes, ct);

        // Send to SUNAT — credentials per tenant (NO global fallback)
        if (string.IsNullOrEmpty(tenant.SunatUser) || string.IsNullOrEmpty(tenant.SunatPasswordEncrypted))
        {
            retention.Status = DocumentStatus.Rejected;
            retention.SunatResponseDescription = "Faltan credenciales SUNAT (SOL user/password) en la configuración del emisor.";
            await _retentionRepo.UpdateAsync(retention, ct);
            return UnprocessableEntity(new { error = "Faltan credenciales SUNAT (SOL) en la configuración del emisor. Configúralas en /settings." });
        }
        var sunatCreds = new SunatCredentials(tenant.SunatUser, _secrets.Unprotect(tenant.SunatPasswordEncrypted), tenant.Environment);
        try
        {
            var zipBytes = CreateZip($"{tenant.Ruc}-20-{retention.FullNumber}.xml", xmlBytes);
            var response = await _sunatClient.SendDocumentAsync(
                tenant.Ruc, "20", retention.FullNumber, zipBytes, sunatCreds, ct);

            retention.SunatResponseCode = response.ResponseCode;
            retention.SunatResponseDescription = response.Description;
            retention.Status = response.Success ? DocumentStatus.Accepted : DocumentStatus.Rejected;

            if (response.CdrZip is not null)
                retention.CdrUrl = await _storageService.UploadCdrAsync(tenantId,
                    $"R-{retention.FullNumber}.zip", response.CdrZip, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send retention {FullNumber} to SUNAT", retention.FullNumber);
            retention.Status = DocumentStatus.Sent;
        }

        await _retentionRepo.UpdateAsync(retention, ct);
        return CreatedAtAction(nameof(GetById), new { id = retention.Id }, MapToResponse(retention));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RetentionResponse>> GetById(Guid id, CancellationToken ct)
    {
        var retention = await _retentionRepo.GetByIdWithReferencesAsync(id, GetTenantId(), ct);
        return retention is null ? NotFound() : Ok(MapToResponse(retention));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _retentionRepo.ListAsync(
            GetTenantId(), page, pageSize, status, dateFrom, dateTo, ct);

        return Ok(new { items = items.Select(MapToResponse), totalCount = total, page, pageSize });
    }

    private static RetentionResponse MapToResponse(RetentionDocument r) => new(
        r.Id, r.Serie, r.Correlative, r.FullNumber, r.IssueDate,
        r.SupplierDocType, r.SupplierDocNumber, r.SupplierName,
        r.RegimeCode, r.RetentionPercent,
        r.TotalInvoiceAmount, r.TotalRetained, r.TotalPaid, r.Currency,
        r.Status, r.SunatResponseCode, r.SunatResponseDescription,
        r.XmlUrl, r.PdfUrl, r.CreatedAt,
        r.References.Select(ref_ => new RetentionReferenceResponse(
            ref_.Id, ref_.DocumentType, ref_.DocumentNumber, ref_.DocumentDate,
            ref_.InvoiceAmount, ref_.PaymentDate, ref_.PaymentAmount,
            ref_.RetainedAmount, ref_.NetPaidAmount)).ToList());

    private static byte[] CreateZip(string fileName, byte[] content)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry(fileName, CompressionLevel.Optimal);
            using var s = entry.Open();
            s.Write(content);
        }
        return ms.ToArray();
    }
}
