using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Domain;

/// <summary>Clean fixture: correctly named <c>&lt;Noun&gt;&lt;PastVerb&gt;DomainEvent</c>.</summary>
public sealed record ShipmentDispatchedDomainEvent(DateTimeOffset OccurredOn) : IDomainEvent;
