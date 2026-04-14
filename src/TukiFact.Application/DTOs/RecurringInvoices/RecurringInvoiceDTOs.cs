using TukiFact.Application.DTOs.Documents;

namespace TukiFact.Application.DTOs.RecurringInvoices;

public record CreateRecurringInvoiceRequest(
    string DocumentType, // "01" or "03"
    string Serie,
    string CustomerDocType,
    string CustomerDocNumber,
    string CustomerName,
    string? CustomerAddress,
    string? CustomerEmail,
    string Currency,
    string Frequency, // daily, weekly, biweekly, monthly, yearly
    int? DayOfMonth, // 1-28 for monthly
    int? DayOfWeek, // 0-6 for weekly
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes,
    List<CreateDocumentItemRequest> Items
);

public record UpdateRecurringInvoiceRequest(
    string? Status, // active, paused, cancelled
    DateOnly? EndDate,
    string? Notes
);

public record RecurringInvoiceResponse(
    Guid Id,
    string DocumentType,
    string Serie,
    string CustomerDocType,
    string CustomerDocNumber,
    string CustomerName,
    string? CustomerEmail,
    string Currency,
    string Frequency,
    int? DayOfMonth,
    int? DayOfWeek,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? NextEmissionDate,
    string Status,
    int EmittedCount,
    DateOnly? LastEmittedDate,
    string? Notes,
    DateTimeOffset CreatedAt
);
