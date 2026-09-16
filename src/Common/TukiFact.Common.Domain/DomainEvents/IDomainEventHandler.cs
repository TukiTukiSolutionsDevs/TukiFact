namespace TukiFact.Common.Domain.DomainEvents;

/// <summary>
/// Handles a domain event inside the same bounded context. Implementations live in the module's
/// Application layer and are dispatched by the in-house mediator (<c>IPublisher</c>) within the transaction.
/// </summary>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
