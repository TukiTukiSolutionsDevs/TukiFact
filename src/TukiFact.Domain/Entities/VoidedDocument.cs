namespace TukiFact.Domain.Entities;

public class VoidedDocument
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TicketType { get; set; } = "RA"; // RA = Comunicación de Baja, RC = Resumen Diario
    public string TicketNumber { get; set; } = string.Empty; // RA-20260407-001
    public DateOnly IssueDate { get; set; }
    public DateOnly ReferenceDate { get; set; }

    // SUNAT async processing
    public string? SunatTicket { get; set; }    // Ticket number from SUNAT
    // Status: pending → signing → sent → accepted | rejected | failed
    public string Status { get; set; } = "pending";
    public string? SunatResponseCode { get; set; }
    public string? SunatResponseDescription { get; set; }

    public string ItemsJson { get; set; } = "[]"; // JSON array of voided/summary items

    // Worker tracking
    public string? XmlUrl { get; set; }          // MinIO path of signed XML
    public string? CdrUrl { get; set; }          // MinIO path of CDR returned by SUNAT
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? LastPolledAt { get; set; }  // Backoff for getStatus polling

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
