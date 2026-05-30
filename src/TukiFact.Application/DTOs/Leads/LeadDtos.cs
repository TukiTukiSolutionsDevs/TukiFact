namespace TukiFact.Application.DTOs.Leads;

public record CreateLeadRequest(
    string Name,
    string Email,
    string? Company,
    string? Phone,
    string? Reason,
    string Message
);

public record LeadResponse(
    Guid Id,
    string Name,
    string Email,
    string? Company,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt
);
