using TukiFact.BuildingBlocks.Contracts;
using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Domain;

/// <summary>Clean fixture in the violating module: Domain type with BCL-only dependencies.</summary>
public sealed class LedgerAccount
{
    public required Guid Id { get; init; }
}

/// <summary>
/// Deliberate violation: a Domain entity must never declare a
/// reserved shadow property name (<c>CreatedAt</c> here) — <c>BaseDbContext</c> owns that column.
/// </summary>
public sealed class LedgerJournalEntry
{
    public required Guid Id { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Deliberate violation: a Domain type must never reference
/// BuildingBlocks.Contracts — that boundary belongs to Application/Infrastructure, not Domain.
/// Correctly named (<c>&lt;Noun&gt;&lt;PastVerb&gt;IntegrationEvent</c>), so it flags only the
/// "integration events live in Contracts" location rule, not the naming rule.
/// </summary>
public sealed record LedgerEntryPostedIntegrationEvent(Guid Id, DateTimeOffset OccurredOn) : IntegrationEvent(Id, OccurredOn);

/// <summary>
/// Deliberate double violation: wrong location (Domain, not
/// Contracts) *and* wrong name (does not end with <c>IntegrationEvent</c>).
/// </summary>
public sealed record LedgerBadEvent(Guid Id, DateTimeOffset OccurredOn) : IntegrationEvent(Id, OccurredOn);

/// <summary>
/// Deliberate violation: a domain event must be named
/// <c>&lt;Noun&gt;&lt;PastVerb&gt;DomainEvent</c> — this one is not.
/// </summary>
public sealed record LedgerStuffHappened(DateTimeOffset OccurredOn) : IDomainEvent;
