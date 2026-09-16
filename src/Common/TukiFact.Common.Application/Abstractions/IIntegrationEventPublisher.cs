using TukiFact.BuildingBlocks.Contracts;

namespace TukiFact.Common.Application.Abstractions;

/// <summary>
/// Outbox port for domain event handlers that translate a domain event into an integration event. The event is
/// stored in the command transaction and reaches NATS JetStream only after that transaction commits; a rollback
/// discards it. Never call it from a command handler or outside a command.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent;
}
