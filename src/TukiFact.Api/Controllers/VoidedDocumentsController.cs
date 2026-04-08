using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Documents;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Enums;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/voided-documents")]
[Authorize(Roles = "admin")]
public class VoidedDocumentsController : ControllerBase
{
    private readonly IVoidedDocumentRepository _voidedRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<VoidedDocumentsController> _logger;

    public VoidedDocumentsController(
        IVoidedDocumentRepository voidedRepo,
        IDocumentRepository documentRepo,
        ITenantProvider tenantProvider,
        ILogger<VoidedDocumentsController> logger)
    {
        _voidedRepo = voidedRepo;
        _documentRepo = documentRepo;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Void (anular) a document via Comunicación de Baja
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> VoidDocument([FromBody] VoidDocumentRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var document = await _documentRepo.GetByIdAsync(request.DocumentId, ct);
        if (document is null)
            return NotFound(new { error = "Documento no encontrado" });

        if (document.Status != DocumentStatus.Accepted)
            return BadRequest(new { error = $"Solo se pueden anular documentos aceptados. Estado actual: {document.Status}" });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var ticketSeq = await _voidedRepo.GetNextTicketNumberAsync(tenantId, "RA", today, ct);
        var ticketNumber = $"RA-{today:yyyyMMdd}-{ticketSeq:D3}";

        var itemsJson = JsonSerializer.Serialize(new[]
        {
            new
            {
                documentType = document.DocumentType,
                serie = document.Serie,
                correlative = document.Correlative,
                fullNumber = document.FullNumber,
                reason = request.VoidReason
            }
        });

        var voided = new VoidedDocument
        {
            TenantId = tenantId,
            TicketType = "RA",
            TicketNumber = ticketNumber,
            IssueDate = today,
            ReferenceDate = document.IssueDate,
            ItemsJson = itemsJson,
            Status = "pending"
        };

        await _voidedRepo.CreateAsync(voided, ct);

        // Update original document status
        document.Status = DocumentStatus.Voided;
        await _documentRepo.UpdateAsync(document, ct);

        _logger.LogInformation("Document {FullNumber} voided with ticket {Ticket}",
            document.FullNumber, ticketNumber);

        return Created($"/v1/voided-documents/{voided.Id}", new VoidedDocumentResponse(
            voided.Id, voided.TicketNumber, voided.Status,
            voided.SunatTicket, voided.SunatResponseCode,
            voided.SunatResponseDescription, voided.CreatedAt));
    }

    /// <summary>
    /// List all voided documents for the current tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var items = await _voidedRepo.GetByTenantAsync(tenantId, ct);
        var response = items.Select(v => new VoidedDocumentResponse(
            v.Id, v.TicketNumber, v.Status, v.SunatTicket,
            v.SunatResponseCode, v.SunatResponseDescription, v.CreatedAt));
        return Ok(response);
    }
}
