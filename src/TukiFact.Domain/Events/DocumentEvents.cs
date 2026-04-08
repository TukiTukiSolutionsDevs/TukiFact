namespace TukiFact.Domain.Events;

public record DocumentCreatedEvent(Guid DocumentId, Guid TenantId, string DocumentType, string FullNumber, decimal Total, string CustomerName);
public record DocumentAcceptedEvent(Guid DocumentId, Guid TenantId, string FullNumber, string? SunatResponseCode);
public record DocumentRejectedEvent(Guid DocumentId, Guid TenantId, string FullNumber, string? SunatResponseCode, string? Description);
public record DocumentVoidedEvent(Guid DocumentId, Guid TenantId, string FullNumber, string VoidReason);
