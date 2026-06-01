using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

/// <summary>
/// Drives Comunicación de Baja (RA) lifecycle for the worker.
/// State machine: pending → signing → sent → accepted | rejected | failed.
/// </summary>
public interface IVoidedDocumentService
{
    /// <summary>Build XML, sign, zip, send to SUNAT via SendSummary. Persists SunatTicket + Status=sent on success.</summary>
    Task SignAndSendAsync(VoidedDocument voided, CancellationToken ct = default);

    /// <summary>Call SUNAT getStatus for a sent ticket. Persists CDR + accepted/rejected when SUNAT returns a final code.</summary>
    Task PollStatusAsync(VoidedDocument voided, CancellationToken ct = default);
}
