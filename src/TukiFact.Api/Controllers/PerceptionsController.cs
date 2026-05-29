using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Perceptions;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Enums;
using TukiFact.Domain.Services;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/perceptions")]
[Authorize]
public class PerceptionsController : ControllerBase
{
    private readonly IPerceptionRepository _perceptionRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IPerceptionXmlBuilder _xmlBuilder;
    private readonly IXmlSigningService _signingService;
    private readonly ISunatClient _sunatClient;
    private readonly IStorageService _storageService;
    private readonly ISecretProtector _secrets;
    private readonly ILogger<PerceptionsController> _logger;

    public PerceptionsController(
        IPerceptionRepository perceptionRepo,
        ITenantRepository tenantRepo,
        IPerceptionXmlBuilder xmlBuilder,
        IXmlSigningService signingService,
        ISunatClient sunatClient,
        IStorageService storageService,
        ISecretProtector secrets,
        ILogger<PerceptionsController> logger)
    {
        _perceptionRepo = perceptionRepo;
        _tenantRepo = tenantRepo;
        _xmlBuilder = xmlBuilder;
        _signingService = signingService;
        _sunatClient = sunatClient;
        _storageService = storageService;
        _secrets = secrets;
        _logger = logger;
    }

    private Guid GetTenantId() => Guid.Parse(User.FindFirstValue("tenant_id")!);

    /// <summary>Create and emit a perception document (tipo 40)</summary>
    [HttpPost]
    public async Task<ActionResult<PerceptionResponse>> Create([FromBody] CreatePerceptionRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        var correlative = await _perceptionRepo.GetNextCorrelativeAsync(tenantId, request.Serie, ct);

        var perception = new PerceptionDocument
        {
            TenantId = tenantId,
            Serie = request.Serie,
            Correlative = correlative,
            IssueDate = request.IssueDate ?? RecurringScheduleCalculator.TodayInLima(),
            CustomerDocType = request.CustomerDocType,
            CustomerDocNumber = request.CustomerDocNumber,
            CustomerName = request.CustomerName,
            CustomerAddress = request.CustomerAddress,
            RegimeCode = request.RegimeCode,
            PerceptionPercent = request.PerceptionPercent,
            Currency = request.Currency ?? "PEN",
            Notes = request.Notes,
            CreatedByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };

        decimal totalInvoice = 0, totalPerceived = 0, totalCollected = 0;

        foreach (var refReq in request.References)
        {
            var perceivedAmount = Math.Round(refReq.CollectionAmount * (request.PerceptionPercent / 100m), 2);
            var totalCollectedAmount = refReq.CollectionAmount + perceivedAmount;

            perception.References.Add(new PerceptionDocumentReference
            {
                DocumentType = refReq.DocumentType,
                DocumentNumber = refReq.DocumentNumber,
                DocumentDate = refReq.DocumentDate,
                InvoiceAmount = refReq.InvoiceAmount,
                InvoiceCurrency = refReq.InvoiceCurrency,
                CollectionDate = refReq.CollectionDate,
                CollectionNumber = refReq.CollectionNumber,
                CollectionAmount = refReq.CollectionAmount,
                PerceivedAmount = perceivedAmount,
                TotalCollectedAmount = totalCollectedAmount,
                ExchangeRate = refReq.ExchangeRate,
                ExchangeRateDate = refReq.ExchangeRateDate
            });

            totalInvoice += refReq.InvoiceAmount;
            totalPerceived += perceivedAmount;
            totalCollected += totalCollectedAmount;
        }

        perception.TotalInvoiceAmount = totalInvoice;
        perception.TotalPerceived = totalPerceived;
        perception.TotalCollected = totalCollected;

        await _perceptionRepo.AddAsync(perception, ct);
        _logger.LogInformation("Perception created: {FullNumber}", perception.FullNumber);

        // Build XML (UBL 2.0)
        var xml = _xmlBuilder.BuildPerceptionXml(perception, tenant);

        // Sign — required; SUNAT rechazará XML sin firma (0306) y dejaría el flujo en estado inconsistente.
        if (tenant.CertificateData is null || tenant.CertificatePasswordEncrypted is null)
        {
            perception.Status = DocumentStatus.Rejected;
            perception.SunatResponseDescription = "El emisor no tiene certificado digital configurado.";
            await _perceptionRepo.UpdateAsync(perception, ct);
            return UnprocessableEntity(new { error = "El emisor no tiene certificado digital configurado. Configúralo en /certificate antes de emitir." });
        }

        string signedXml;
        try
        {
            var (signed, digest) = _signingService.SignXml(xml, tenant.CertificateData, _secrets.Unprotect(tenant.CertificatePasswordEncrypted));
            signedXml = signed;
            perception.HashCode = digest;
            perception.Status = DocumentStatus.Signed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign perception {FullNumber}", perception.FullNumber);
            perception.Status = DocumentStatus.Rejected;
            perception.SunatResponseDescription = "Error firmando XML: " + ex.Message;
            await _perceptionRepo.UpdateAsync(perception, ct);
            return UnprocessableEntity(new { error = "Error firmando XML", detail = ex.Message });
        }

        // Store XML
        var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
        perception.XmlUrl = await _storageService.UploadXmlAsync(tenantId,
            $"{perception.FullNumber}.xml", xmlBytes, ct);

        // Send to SUNAT — credentials per tenant (NO global fallback; cada tenant tiene su SOL user/pass)
        if (string.IsNullOrEmpty(tenant.SunatUser) || string.IsNullOrEmpty(tenant.SunatPasswordEncrypted))
        {
            perception.Status = DocumentStatus.Rejected;
            perception.SunatResponseDescription = "Faltan credenciales SUNAT (SOL user/password) en la configuración del emisor.";
            await _perceptionRepo.UpdateAsync(perception, ct);
            return UnprocessableEntity(new { error = "Faltan credenciales SUNAT (SOL) en la configuración del emisor. Configúralas en /settings." });
        }
        var sunatCreds = new SunatCredentials(tenant.SunatUser, _secrets.Unprotect(tenant.SunatPasswordEncrypted), tenant.Environment);
        try
        {
            var zipBytes = CreateZip($"{tenant.Ruc}-40-{perception.FullNumber}.xml", xmlBytes);
            var response = await _sunatClient.SendDocumentAsync(
                tenant.Ruc, "40", perception.FullNumber, zipBytes, sunatCreds, ct);

            perception.SunatResponseCode = response.ResponseCode;
            perception.SunatResponseDescription = response.Description;
            perception.Status = response.Success ? DocumentStatus.Accepted : DocumentStatus.Rejected;

            if (response.CdrZip is not null)
                perception.CdrUrl = await _storageService.UploadCdrAsync(tenantId,
                    $"R-{perception.FullNumber}.zip", response.CdrZip, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send perception {FullNumber} to SUNAT", perception.FullNumber);
            perception.Status = DocumentStatus.Sent;
        }

        await _perceptionRepo.UpdateAsync(perception, ct);
        return CreatedAtAction(nameof(GetById), new { id = perception.Id }, MapToResponse(perception));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PerceptionResponse>> GetById(Guid id, CancellationToken ct)
    {
        var perception = await _perceptionRepo.GetByIdWithReferencesAsync(id, GetTenantId(), ct);
        return perception is null ? NotFound() : Ok(MapToResponse(perception));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _perceptionRepo.ListAsync(
            GetTenantId(), page, pageSize, status, dateFrom, dateTo, ct);

        return Ok(new { items = items.Select(MapToResponse), totalCount = total, page, pageSize });
    }

    private static PerceptionResponse MapToResponse(PerceptionDocument p) => new(
        p.Id, p.Serie, p.Correlative, p.FullNumber, p.IssueDate,
        p.CustomerDocType, p.CustomerDocNumber, p.CustomerName,
        p.RegimeCode, p.PerceptionPercent,
        p.TotalInvoiceAmount, p.TotalPerceived, p.TotalCollected, p.Currency,
        p.Status, p.SunatResponseCode, p.SunatResponseDescription,
        p.XmlUrl, p.PdfUrl, p.CreatedAt,
        p.References.Select(ref_ => new PerceptionReferenceResponse(
            ref_.Id, ref_.DocumentType, ref_.DocumentNumber, ref_.DocumentDate,
            ref_.InvoiceAmount, ref_.CollectionDate, ref_.CollectionAmount,
            ref_.PerceivedAmount, ref_.TotalCollectedAmount)).ToList());

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
