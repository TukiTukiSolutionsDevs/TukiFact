namespace TukiFact.Application.DTOs.Billing;

public record SubscribeRequest(
    string Token,           // Opaque Culqi token from frontend (tkn_...)
    Guid PlanId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string CountryCode = "PE"
);

public record CancelSubscriptionRequest(
    string? Reason
);

public record ChangePlanRequest(
    Guid NewPlanId
);

public record SubscriptionResponse(
    Guid Id,
    Guid TenantId,
    Guid PlanId,
    string PlanName,
    string Status,
    decimal MonthlyAmount,
    int DocumentsLimit,
    int DocumentsUsedThisMonth,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset NextBillingDate,
    DateTimeOffset? LastChargedAt,
    string? LastChargeId,
    bool IsCulqiManaged
);
