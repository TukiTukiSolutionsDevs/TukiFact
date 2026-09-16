using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.Common.Domain.Entities;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();

    void ClearDomainEvents();
}
