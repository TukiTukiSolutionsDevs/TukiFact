namespace TukiFact.BuildingBlocks.Contracts;

/// <summary>
/// Base record for integration events: <c>&lt;Noun&gt;&lt;PastVerb&gt;IntegrationEvent(Guid Id, DateTimeOffset OccurredOn, ...)</c>.
/// Payloads carry identifiers and values only, never domain entities.
/// </summary>
public abstract record IntegrationEvent(Guid Id, DateTimeOffset OccurredOn) : IIntegrationEvent
{
    public string? CorrelationId { get; init; }

    public virtual int Version => 1;
}
