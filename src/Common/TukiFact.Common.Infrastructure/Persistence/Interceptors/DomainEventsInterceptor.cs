using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.DomainEvents;
using TukiFact.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TukiFact.Common.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Dispatches the domain events of tracked aggregates through <see cref="IPublisher"/> while saving, before
/// rows are written and before the transaction commits, so handler effects join the same transaction.
/// Events are cleared before dispatch, hence published once per SaveChanges.
/// </summary>
internal sealed class DomainEventsInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        throw new NotSupportedException("Domain events are dispatched asynchronously; use SaveChangesAsync.");

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is not null)
        {
            await DispatchAsync(eventData.Context, cancellationToken);
        }

        return result;
    }

    private async Task DispatchAsync(DbContext context, CancellationToken cancellationToken)
    {
        // Handlers may raise new events on tracked aggregates; drain until none remain.
        var domainEvents = TakeDomainEvents(context);
        while (domainEvents.Count > 0)
        {
            foreach (var domainEvent in domainEvents)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }

            domainEvents = TakeDomainEvents(context);
        }
    }

    private static List<IDomainEvent> TakeDomainEvents(DbContext context)
    {
        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(aggregate => aggregate.GetDomainEvents()).ToList();
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());

        return domainEvents;
    }
}
