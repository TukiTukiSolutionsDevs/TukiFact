using System.Collections.Concurrent;
using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>Records dispatched domain events per order so tests stay isolated by unique ids.</summary>
public sealed class DomainEventLog
{
    private readonly ConcurrentQueue<(Guid OrderId, IDomainEvent Event)> _entries = new();

    public void Record(Guid orderId, IDomainEvent domainEvent) => _entries.Enqueue((orderId, domainEvent));

    public IReadOnlyList<IDomainEvent> For(Guid orderId) =>
        [.. _entries.Where(entry => entry.OrderId == orderId).Select(entry => entry.Event)];
}
