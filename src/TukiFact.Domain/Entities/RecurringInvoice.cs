namespace TukiFact.Domain.Entities;

public class RecurringInvoice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Template document info
    public string DocumentType { get; set; } = "01"; // 01=Factura, 03=Boleta
    public string Serie { get; set; } = string.Empty; // F001, B001

    // Customer
    public string CustomerDocType { get; set; } = "6";
    public string CustomerDocNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }

    // Items template (stored as JSON)
    public string ItemsJson { get; set; } = "[]";

    // Currency
    public string Currency { get; set; } = "PEN";

    // Scheduling
    public string Frequency { get; set; } = "monthly"; // daily, weekly, biweekly, monthly, yearly
    public int? DayOfMonth { get; set; }                // 1-28 for monthly
    public int? DayOfWeek { get; set; }                 // 0-6 for weekly (0=Sunday)
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? NextEmissionDate { get; set; }

    // Status
    public string Status { get; set; } = "active"; // active, paused, cancelled, completed

    // Stats
    public int EmittedCount { get; set; }
    public DateOnly? LastEmittedDate { get; set; }

    // Notes
    public string? Notes { get; set; }

    // Audit
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
