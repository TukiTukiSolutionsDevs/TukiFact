using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class KernelTestOrderRelabeledDomainEventHandler(DomainEventLog log)
    : IDomainEventHandler<KernelTestOrderRelabeledDomainEvent>
{
    public Task Handle(KernelTestOrderRelabeledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        log.Record(domainEvent.OrderId, domainEvent);
        return Task.CompletedTask;
    }
}
