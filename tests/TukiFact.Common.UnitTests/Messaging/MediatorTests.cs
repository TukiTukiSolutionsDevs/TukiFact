using Microsoft.Extensions.DependencyInjection;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.DomainEvents;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.UnitTests.Fakes;
using TukiFact.Common.UnitTests.Messaging.Fakes;

namespace TukiFact.Common.UnitTests.Messaging;

public sealed class MediatorTests
{
    [Fact]
    public async Task Send_CommandWithSingleHandler_ReturnsHandlerResult()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var orderId = Guid.NewGuid();

        // Act
        var result = await sender.Send(new PlaceOrderCommand(orderId), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(orderId);
    }

    [Fact]
    public async Task Send_QueryWithSingleHandler_ReturnsHandlerResult()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var result = await sender.Send(new GetOrderStatusQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(GetOrderStatusQueryHandler.Status);
    }

    [Fact]
    public async Task Send_RequestWithoutHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var act = () => sender.Send(new UnhandledCommand(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Send_WithAdditionalBehaviorsRegisteredAfterAddCommonApplication_RunsThemAfterTheFixedPipeline()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build(services =>
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(OuterRecordingBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InnerRecordingBehavior<,>));
        });
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        await sender.Send(new PlaceOrderCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert: the two recording behaviors run in registration order, after the four fixed ones, before the handler.
        var entries = scope.ServiceProvider.GetRequiredService<CallLog>().Entries;
        entries.Should().Equal(
            OuterRecordingBehavior<PlaceOrderCommand, Result<Guid>>.Entry,
            InnerRecordingBehavior<PlaceOrderCommand, Result<Guid>>.Entry,
            nameof(PlaceOrderCommandHandler));
    }

    [Fact]
    public async Task Publish_NotificationWithoutHandlers_Completes()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Act
        var act = () => publisher.Publish(new OrphanNotification(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_NotificationWithTwoHandlers_InvokesEachHandler()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Act
        await publisher.Publish(new StockReplenishedNotification(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        scope.ServiceProvider.GetRequiredService<CallLog>().Entries.Should().BeEquivalentTo(
            nameof(FirstStockReplenishedHandler),
            nameof(SecondStockReplenishedHandler));
    }

    [Fact]
    public async Task Publish_DomainEventTypedAsInterface_InvokesEachHandlerOfRuntimeType()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        IDomainEvent domainEvent = new OrderShippedDomainEvent(DateTimeOffset.UnixEpoch);

        // Act
        await publisher.Publish(domainEvent, TestContext.Current.CancellationToken);

        // Assert
        scope.ServiceProvider.GetRequiredService<CallLog>().Entries.Should().BeEquivalentTo(
            nameof(FirstOrderShippedHandler),
            nameof(SecondOrderShippedHandler));
    }
}
