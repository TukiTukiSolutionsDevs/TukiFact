using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class KernelTestOrderRegisteredDomainEventHandler(DomainEventLog log)
    : IDomainEventHandler<KernelTestOrderRegisteredDomainEvent>
{
    /// <summary>Reference that makes this handler fail, to prove handlers run inside the command transaction.</summary>
    public const string RejectedReference = "rejected-by-event-handler";

    public Task Handle(KernelTestOrderRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        log.Record(domainEvent.OrderId, domainEvent);

        return domainEvent.Reference == RejectedReference
            ? Task.FromException(new InvalidOperationException("Event handler rejected the order."))
            : Task.CompletedTask;
    }
}
