using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Documents;
using TukiFact.Application.Interfaces;
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
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _documentService.EmitAsync(request, tenantId, ct);

            _logger.LogInformation("Document emitted: {FullNumber} Status: {Status}",
                result.FullNumber, result.Status);

            return Created($"/v1/documents/{result.Id}", result);
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
        var result = await _documentService.GetByIdAsync(id, ct);
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
        var doc = await _documentService.GetByIdAsync(id, ct);
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
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _documentService.EmitCreditNoteAsync(request, tenantId, ct);
            _logger.LogInformation("Credit note emitted: {FullNumber}", result.FullNumber);
            return Created($"/v1/documents/{result.Id}", result);
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
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _documentService.EmitDebitNoteAsync(request, tenantId, ct);
            _logger.LogInformation("Debit note emitted: {FullNumber}", result.FullNumber);
            return Created($"/v1/documents/{result.Id}", result);
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
        CancellationToken ct)
    {
        var doc = await _documentService.GetByIdAsync(id, ct);
        if (doc is null) return NotFound();

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var documentRepo = HttpContext.RequestServices
            .GetRequiredService<IDocumentRepository>();
        var tenantRepo = HttpContext.RequestServices
            .GetRequiredService<ITenantRepository>();

        var document = await documentRepo.GetByIdWithItemsAsync(id, ct);
        var tenant = await tenantRepo.GetByIdAsync(tenantId, ct);
        if (document is null || tenant is null) return NotFound();

        var pdfBytes = pdfGenerator.GenerateInvoicePdf(document, tenant);
        return File(pdfBytes, "application/pdf", $"{doc.FullNumber}.pdf");
    }
}
