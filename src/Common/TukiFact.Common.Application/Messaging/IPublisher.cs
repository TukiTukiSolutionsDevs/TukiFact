using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.Common.Application.Messaging;

public interface IPublisher
{
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>Dispatches to every <see cref="IDomainEventHandler{TDomainEvent}"/> of the event's runtime type.</summary>
    Task Publish(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
