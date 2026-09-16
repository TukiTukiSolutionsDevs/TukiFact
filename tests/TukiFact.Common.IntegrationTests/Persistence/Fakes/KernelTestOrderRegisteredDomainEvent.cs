using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

public sealed record KernelTestOrderRegisteredDomainEvent(Guid OrderId, Guid TenantId, string Reference, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
