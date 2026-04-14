using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Quotations;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/quotations")]
[Authorize]
public class QuotationsController : ControllerBase
{
    private readonly IQuotationRepository _quotationRepo;
    private readonly IDocumentService _documentService;
    private readonly ILogger<QuotationsController> _logger;
    private const decimal IgvRate = 0.18m;

    public QuotationsController(
        IQuotationRepository quotationRepo,
        IDocumentService documentService,
        ILogger<QuotationsController> logger)
    {
        _quotationRepo = quotationRepo;
        _documentService = documentService;
        _logger = logger;
    }

    private Guid GetTenantId() => Guid.Parse(User.FindFirstValue("tenant_id")!);

    [HttpPost]
    public async Task<ActionResult<QuotationResponse>> Create([FromBody] CreateQuotationRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var correlative = await _quotationRepo.GetNextCorrelativeAsync(tenantId, ct);

        var quotation = new Quotation
        {
            TenantId = tenantId,
            QuotationNumber = $"COT-{correlative:D6}",
            Correlative = correlative,
            IssueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = request.ValidUntil,
            CustomerDocType = request.CustomerDocType,
            CustomerDocNumber = request.CustomerDocNumber,
            CustomerName = request.CustomerName,
            CustomerAddress = request.CustomerAddress,
            CustomerEmail = request.CustomerEmail,
            CustomerPhone = request.CustomerPhone,
            Currency = request.Currency ?? "PEN",
            Notes = request.Notes,
            TermsAndConditions = request.TermsAndConditions,
            CreatedByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };

        // Build items and calculate totals
        decimal totalSubtotal = 0, totalIgv = 0, totalDiscount = 0;
        int seq = 1;

        foreach (var itemReq in request.Items)
        {
            var subtotal = Math.Round(itemReq.Quantity * itemReq.UnitPrice, 2);
            var discount = Math.Round(itemReq.Discount, 2);
            var taxable = subtotal - discount;
            var igv = itemReq.IgvType == "10" ? Math.Round(taxable * IgvRate, 2) : 0;

            quotation.Items.Add(new QuotationItem
            {
                Sequence = seq++,
                ProductCode = itemReq.ProductCode,
                Description = itemReq.Description,
                Quantity = itemReq.Quantity,
                UnitMeasure = itemReq.UnitMeasure ?? "NIU",
                UnitPrice = itemReq.UnitPrice,
                Discount = discount,
                IgvType = itemReq.IgvType,
                IgvAmount = igv,
                Subtotal = taxable,
                Total = taxable + igv
            });

            totalSubtotal += taxable;
            totalIgv += igv;
            totalDiscount += discount;
        }

        quotation.Subtotal = totalSubtotal;
        quotation.Igv = totalIgv;
        quotation.Total = totalSubtotal + totalIgv;
        quotation.TotalDiscount = totalDiscount;

        await _quotationRepo.AddAsync(quotation, ct);
        _logger.LogInformation("Quotation created: {Number}", quotation.QuotationNumber);

        return CreatedAtAction(nameof(GetById), new { id = quotation.Id }, MapToResponse(quotation));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationResponse>> GetById(Guid id, CancellationToken ct)
    {
        var quotation = await _quotationRepo.GetByIdWithItemsAsync(id, ct);
        return quotation is null ? NotFound() : Ok(MapToResponse(quotation));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _quotationRepo.ListAsync(
            GetTenantId(), page, pageSize, status, dateFrom, dateTo, ct);
        return Ok(new { items = items.Select(MapToResponse), totalCount = total, page, pageSize });
    }

    /// <summary>Update quotation status</summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateQuotationStatusRequest request, CancellationToken ct)
    {
        var quotation = await _quotationRepo.GetByIdWithItemsAsync(id, ct);
        if (quotation is null) return NotFound();
        if (quotation.TenantId != GetTenantId()) return Forbid();

        quotation.Status = request.Status;
        await _quotationRepo.UpdateAsync(quotation, ct);
        return Ok(MapToResponse(quotation));
    }

    /// <summary>Convert quotation to invoice — DIFERENCIADOR</summary>
    [HttpPost("{id:guid}/convert-to-invoice")]
    public async Task<IActionResult> ConvertToInvoice(Guid id, [FromBody] ConvertToInvoiceRequest request, CancellationToken ct)
    {
        var quotation = await _quotationRepo.GetByIdWithItemsAsync(id, ct);
        if (quotation is null) return NotFound();
        if (quotation.TenantId != GetTenantId()) return Forbid();
        if (quotation.Status == "invoiced")
            return BadRequest(new { error = "Esta cotización ya fue convertida a factura" });

        // Build document items from quotation items
        var items = quotation.Items.OrderBy(i => i.Sequence).Select(i =>
            new Application.DTOs.Documents.CreateDocumentItemRequest(
                i.ProductCode, null, i.Description, i.Quantity,
                i.UnitMeasure, i.UnitPrice, i.IgvType, i.Discount)).ToList();

        var docRequest = new Application.DTOs.Documents.CreateDocumentRequest(
            request.DocumentType ?? "01",
            request.Serie,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            quotation.Currency,
            quotation.CustomerDocType,
            quotation.CustomerDocNumber,
            quotation.CustomerName,
            quotation.CustomerAddress,
            quotation.CustomerEmail,
            $"Generado desde cotización {quotation.QuotationNumber}",
            null,
            items
        );

        var result = await _documentService.EmitAsync(docRequest, quotation.TenantId, ct);

        // Update quotation
        quotation.Status = "invoiced";
        quotation.InvoiceDocumentId = result.Id;
        quotation.InvoiceDocumentNumber = result.FullNumber;
        await _quotationRepo.UpdateAsync(quotation, ct);

        _logger.LogInformation("Quotation {QuotNumber} converted to invoice {InvNumber}",
            quotation.QuotationNumber, result.FullNumber);

        return Ok(new
        {
            quotation = MapToResponse(quotation),
            invoice = result
        });
    }

    private static QuotationResponse MapToResponse(Quotation q) => new(
        q.Id, q.QuotationNumber, q.Correlative, q.IssueDate, q.ValidUntil,
        q.CustomerDocType, q.CustomerDocNumber, q.CustomerName, q.CustomerEmail,
        q.Currency, q.Subtotal, q.Igv, q.Total,
        q.Status, q.InvoiceDocumentId, q.InvoiceDocumentNumber,
        q.PdfUrl, q.Notes, q.CreatedAt,
        q.Items.OrderBy(i => i.Sequence).Select(i => new QuotationItemResponse(
            i.Sequence, i.ProductCode, i.Description, i.Quantity,
            i.UnitMeasure, i.UnitPrice, i.IgvType, i.IgvAmount,
            i.Subtotal, i.Total)).ToList());
}

public record UpdateQuotationStatusRequest(string Status);
public record ConvertToInvoiceRequest(string Serie, string? DocumentType);
