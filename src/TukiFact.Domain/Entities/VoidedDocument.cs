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
    public string Status { get; set; } = "pending"; // pending, processing, accepted, rejected
    public string? SunatResponseCode { get; set; }
    public string? SunatResponseDescription { get; set; }

    public string ItemsJson { get; set; } = "[]"; // JSON array of voided/summary items

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
