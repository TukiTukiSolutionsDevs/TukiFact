namespace TukiFact.Common.Domain.DomainEvents;

/// <summary>Base domain event; the aggregate supplies <see cref="OccurredOn"/> from the clock it received.</summary>
public abstract record DomainEvent(DateTimeOffset OccurredOn) : IDomainEvent;
