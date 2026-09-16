using TukiFact.Common.Domain.DomainEvents;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Messaging.Fakes;

internal sealed class SecondOrderShippedHandler(CallLog log) : IDomainEventHandler<OrderShippedDomainEvent>
{
    public Task Handle(OrderShippedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        log.Add(nameof(SecondOrderShippedHandler));
        return Task.CompletedTask;
    }
}
