namespace TukiFact.Application.DTOs.Webhooks;

public record CreateWebhookRequest(string Url, string[] Events, int MaxRetries = 3);
public record UpdateWebhookRequest(string? Url, string[]? Events, bool? IsActive, int? MaxRetries);

public record WebhookConfigResponse(
    Guid Id, string Url, string[] Events, bool IsActive, int MaxRetries,
    DateTimeOffset? LastTriggeredAt, DateTimeOffset CreatedAt
);

public record WebhookDeliveryResponse(
    Guid Id, string EventType, string Status, int Attempt,
    string? ResponseStatus, DateTimeOffset CreatedAt
);
