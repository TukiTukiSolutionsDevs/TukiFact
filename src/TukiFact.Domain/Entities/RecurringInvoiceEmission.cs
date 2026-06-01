namespace TukiFact.Domain.Entities;

/// <summary>
/// One row per scheduled emission attempt. The unique index on (RecurringInvoiceId, TargetDate)
/// is the idempotency key — a crash mid-emit followed by a retry sees the existing row and skips,
/// so a single scheduled date can never produce two SUNAT submissions.
/// </summary>
public class RecurringInvoiceEmission
{
    public Guid Id { get; set; }
    public Guid RecurringInvoiceId { get; set; }

    /// <summary>The schedule's NextEmissionDate at the moment of claim. Drives idempotency.</summary>
    public DateOnly TargetDate { get; set; }

    /// <summary>FK to the materialized Document, set after the emission completes (success or failure).</summary>
    public Guid? DocumentId { get; set; }

    /// <summary>pending → succeeded | rejected | error</summary>
    public string Status { get; set; } = "pending";

    /// <summary>SUNAT response code when available (e.g. "0", "2200", etc.).</summary>
    public string? SunatResponseCode { get; set; }

    /// <summary>Free-text error captured from the catch block or SUNAT description.</summary>
    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation
    public RecurringInvoice RecurringInvoice { get; set; } = null!;
}
