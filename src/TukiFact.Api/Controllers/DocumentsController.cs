using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Documents;
using TukiFact.Application.Exceptions;
using TukiFact.Application.Interfaces;
using TukiFact.Application.Validation;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ITenantProvider tenantProvider,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Emit a new electronic document (Factura or Boleta)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> Emit([FromBody] CreateDocumentRequest request, CancellationToken ct)
    {
        var validationErrors = DocumentValidator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { error = "Datos inválidos para emitir el documento.", details = validationErrors });

        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _documentService.EmitAsync(request, tenantId, ct);

            _logger.LogInformation("Document emitted: {FullNumber} Status: {Status}",
                result.FullNumber, result.Status);

            return Created($"/v1/documents/{result.Id}", result);
        }
        catch (PlanLimitExceededException ex)
        {
            return StatusCode(402, new
            {
                error = ex.Message,
                code = "plan_limit_exceeded",
                plan = ex.PlanName,
                monthlyLimit = ex.MonthlyLimit,
                currentCount = ex.CurrentCount,
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emitting document");
            return StatusCode(500, new { error = "Error al emitir documento", detail = ex.Message });
        }
    }

    /// <summary>
    /// Get a document by ID with all its items
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var result = await _documentService.GetByIdAsync(id, tenantId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// List documents with filters and pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? documentType = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var (items, totalCount) = await _documentService.ListAsync(
            tenantId, page, pageSize, documentType, status, dateFrom, dateTo, ct);

        return Ok(new
        {
            data = items,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Download the XML of a document
    /// </summary>
    [HttpGet("{id:guid}/xml")]
    public async Task<IActionResult> DownloadXml(Guid id, [FromServices] IStorageService storageService, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var doc = await _documentService.GetByIdAsync(id, tenantId, ct);
        if (doc?.XmlUrl is null) return NotFound();

        var parts = doc.XmlUrl.Split('/', 2);
        if (parts.Length != 2) return NotFound();

        var content = await storageService.DownloadAsync(parts[0], parts[1], ct);
        if (content is null) return NotFound();

        return File(content, "application/xml", $"{doc.FullNumber}.xml");
    }

    /// <summary>
    /// Emit a Credit Note (Nota de Crédito)
    /// </summary>
    [HttpPost("credit-note")]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> EmitCreditNote([FromBody] CreateCreditNoteRequest request, CancellationToken ct)
    {
        var validationErrors = DocumentValidator.ValidateCreditNote(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { error = "Datos inválidos para emitir la nota de crédito.", details = validationErrors });

        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _documentService.EmitCreditNoteAsync(request, tenantId, ct);
            _logger.LogInformation("Credit note emitted: {FullNumber}", result.FullNumber);
            return Created($"/v1/documents/{result.Id}", result);
        }
        catch (PlanLimitExceededException ex)
        {
            return StatusCode(402, new
            {
                error = ex.Message,
                code = "plan_limit_exceeded",
                plan = ex.PlanName,
                monthlyLimit = ex.MonthlyLimit,
                currentCount = ex.CurrentCount,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Emit a Debit Note (Nota de Débito)
    /// </summary>
    [HttpPost("debit-note")]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> EmitDebitNote([FromBody] CreateDebitNoteRequest request, CancellationToken ct)
    {
        var validationErrors = DocumentValidator.ValidateDebitNote(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { error = "Datos inválidos para emitir la nota de débito.", details = validationErrors });

        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _documentService.EmitDebitNoteAsync(request, tenantId, ct);
            _logger.LogInformation("Debit note emitted: {FullNumber}", result.FullNumber);
            return Created($"/v1/documents/{result.Id}", result);
        }
        catch (PlanLimitExceededException ex)
        {
            return StatusCode(402, new
            {
                error = ex.Message,
                code = "plan_limit_exceeded",
                plan = ex.PlanName,
                monthlyLimit = ex.MonthlyLimit,
                currentCount = ex.CurrentCount,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download PDF representation of a document
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromServices] IPdfGenerator pdfGenerator,
        [FromServices] IStorageService storage,
        CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var doc = await _documentService.GetByIdAsync(id, tenantId, ct);
        if (doc is null) return NotFound();

        var documentRepo = HttpContext.RequestServices
            .GetRequiredService<IDocumentRepository>();
        var tenantRepo = HttpContext.RequestServices
            .GetRequiredService<ITenantRepository>();

        var document = await documentRepo.GetByIdWithItemsAsync(id, tenantId, ct);
        var tenant = await tenantRepo.GetByIdAsync(tenantId, ct);
        if (document is null || tenant is null) return NotFound();

        // Prefer persisted PDF from MinIO (avoids re-rendering on every request).
        // Falls back to on-demand render for legacy docs emitted before PDF-on-accept.
        if (!string.IsNullOrEmpty(document.PdfUrl))
        {
            var slash = document.PdfUrl.IndexOf('/');
            if (slash > 0)
            {
                var bucket = document.PdfUrl[..slash];
                var objectName = document.PdfUrl[(slash + 1)..];
                var stored = await storage.DownloadAsync(bucket, objectName, ct);
                if (stored is not null)
                    return File(stored, "application/pdf", $"{doc.FullNumber}.pdf");
            }
        }

        var pdfBytes = pdfGenerator.GenerateInvoicePdf(document, tenant);
        return File(pdfBytes, "application/pdf", $"{doc.FullNumber}.pdf");
    }
}
