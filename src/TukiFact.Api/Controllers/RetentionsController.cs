using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Retentions;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Enums;

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
    private readonly ILogger<RetentionsController> _logger;

    public RetentionsController(
        IRetentionRepository retentionRepo,
        ITenantRepository tenantRepo,
        IRetentionXmlBuilder xmlBuilder,
        IXmlSigningService signingService,
        ISunatClient sunatClient,
        IStorageService storageService,
        ILogger<RetentionsController> logger)
    {
        _retentionRepo = retentionRepo;
        _tenantRepo = tenantRepo;
        _xmlBuilder = xmlBuilder;
        _signingService = signingService;
        _sunatClient = sunatClient;
        _storageService = storageService;
        _logger = logger;
    }

    private Guid GetTenantId() => Guid.Parse(User.FindFirstValue("tenant_id")!);

    /// <summary>Create and emit a retention document (tipo 20)</summary>
    [HttpPost]
    public async Task<ActionResult<RetentionResponse>> Create([FromBody] CreateRetentionRequest request, CancellationToken ct)
    {
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
            IssueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
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

        // Sign XML
        string signedXml = xml;
        if (tenant.CertificateData is not null && tenant.CertificatePasswordEncrypted is not null)
        {
            try
            {
                var (signed, digest) = _signingService.SignXml(xml, tenant.CertificateData, tenant.CertificatePasswordEncrypted);
                signedXml = signed;
                retention.HashCode = digest;
                retention.Status = DocumentStatus.Signed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sign retention {FullNumber}", retention.FullNumber);
            }
        }

        // Store XML
        var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
        retention.XmlUrl = await _storageService.UploadXmlAsync(tenantId,
            $"{retention.FullNumber}.xml", xmlBytes, ct);

        // Send to SUNAT (endpoint separado para retención/percepción)
        try
        {
            var zipBytes = CreateZip($"{tenant.Ruc}-20-{retention.FullNumber}.xml", xmlBytes);
            var response = await _sunatClient.SendDocumentAsync(
                tenant.Ruc, "20", retention.FullNumber, zipBytes, ct);

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
        var retention = await _retentionRepo.GetByIdWithReferencesAsync(id, ct);
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
