using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.Common.UnitTests.Messaging.Fakes;

internal sealed record OrderShippedDomainEvent(DateTimeOffset OccurredOn) : IDomainEvent;
