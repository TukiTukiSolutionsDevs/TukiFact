using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

public sealed record KernelTestOrderRelabeledDomainEvent(Guid OrderId, string Reference, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
