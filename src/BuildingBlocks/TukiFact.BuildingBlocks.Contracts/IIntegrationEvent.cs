namespace TukiFact.BuildingBlocks.Contracts;

/// <summary>
/// An event that crosses module boundaries through the durable Postgres outbox and NATS JetStream.
/// Raised by a domain event handler that translates a domain event; never published straight from a command handler.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique per fact. Consumers deduplicate on it (idempotent inbox).</summary>
    Guid Id { get; }

    /// <summary>When the underlying domain fact happened (UTC, from <c>IClock</c>).</summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// <c>X-Correlation-Id</c> of the request that produced the fact. Stamped by the publisher when left empty,
    /// so the chain HTTP → outbox → NATS consumer keeps one id.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>Contract version of the payload; a breaking change adds a new event type or bumps this value.</summary>
    int Version { get; }
}
