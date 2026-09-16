using TukiFact.BuildingBlocks.Contracts;
using TukiFact.Common.Application.Abstractions;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>
/// The kernel persistence collection runs without an outbox transport (that lands with the messaging PR).
/// Registered only so the container validates the domain event handlers the fake module declares.
/// </summary>
internal sealed class UnconfiguredIntegrationEventPublisher : IIntegrationEventPublisher
{
    public Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent =>
        throw new InvalidOperationException("Integration event publishing is not configured in this test collection.");
}
