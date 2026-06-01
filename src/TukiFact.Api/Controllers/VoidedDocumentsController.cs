using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Documents;
using TukiFact.Application.Interfaces;
using TukiFact.Application.Validation;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Enums;
using TukiFact.Domain.Interfaces;
using TukiFact.Domain.Services;

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
        var validationErrors = VoidDocumentValidator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { error = "Datos inválidos para la anulación.", details = validationErrors });

        var tenantId = _tenantProvider.GetCurrentTenantId();

        var document = await _documentRepo.GetByIdAsync(request.DocumentId, tenantId, ct);
        if (document is null)
            return NotFound(new { error = "Documento no encontrado" });

        if (document.Status != DocumentStatus.Accepted)
            return BadRequest(new { error = $"Solo se pueden anular documentos aceptados. Estado actual: {document.Status}" });

        // SUNAT only accepts Comunicación de Baja (RA) for invoices, credit notes, debit notes.
        // Boletas (03) are voided via the daily summary (RC), which is a separate flow not yet wired here.
        if (document.DocumentType is not ("01" or "07" or "08"))
        {
            return BadRequest(new
            {
                error = $"Tipo de documento {document.DocumentType} no soportado para Comunicación de Baja. " +
                        "Las boletas se anulan vía Resumen Diario (próximamente)."
            });
        }

        var today = RecurringScheduleCalculator.TodayInLima();

        // SUNAT plazo: la Comunicación de Baja debe enviarse hasta el 7° día calendario posterior a la emisión.
        var daysSinceIssue = today.DayNumber - document.IssueDate.DayNumber;
        if (daysSinceIssue > 7)
        {
            return BadRequest(new
            {
                error = $"Plazo de anulación vencido. SUNAT acepta Comunicación de Baja hasta 7 días calendario después " +
                        $"de la emisión (emitido hace {daysSinceIssue} días). Use Nota de Crédito en su lugar."
            });
        }

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
            IssueDate = today,
            ReferenceDate = document.IssueDate,
            ItemsJson = itemsJson,
            Status = "pending"
            // TicketNumber assigned race-safely below via CreateWithTicketAsync (advisory lock).
        };

        await _voidedRepo.CreateWithTicketAsync(voided, ct);

        // Update original document status
        document.Status = DocumentStatus.Voided;
        await _documentRepo.UpdateAsync(document, ct);

        _logger.LogInformation("Document {FullNumber} voided with ticket {Ticket}",
            document.FullNumber, voided.TicketNumber);

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
