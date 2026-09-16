using Microsoft.Extensions.DependencyInjection;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.UnitTests.Behaviors.Fakes;
using TukiFact.Common.UnitTests.Fakes;
using TukiFact.Common.UnitTests.Messaging.Fakes;

namespace TukiFact.Common.UnitTests.Behaviors;

public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Send_SuccessfulCommand_CommitsTransaction()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        await sender.Send(new PlaceOrderCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        scope.ServiceProvider.GetRequiredService<FakeTransactionManager>().Calls.Should().Equal(
            FakeTransactionManager.Begin,
            FakeTransactionManager.Commit);
    }

    [Fact]
    public async Task Send_FailedCommand_RollsBackWithoutCommitting()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var result = await sender.Send(new RejectOrderCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Should().Be(RejectOrderCommandHandler.AlreadyShipped);
        scope.ServiceProvider.GetRequiredService<FakeTransactionManager>().Calls.Should().Equal(
            FakeTransactionManager.Begin,
            FakeTransactionManager.Rollback);
    }

    [Fact]
    public async Task Send_CommandHandlerThrows_RollsBackAndRethrows()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var act = () => sender.Send(new CrashOrderCommand(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        scope.ServiceProvider.GetRequiredService<FakeTransactionManager>().Calls.Should().Equal(
            FakeTransactionManager.Begin,
            FakeTransactionManager.Rollback);
    }

    [Fact]
    public async Task Send_HandlerCancelsTokenBeforeReturningSuccess_StillCommitsTransaction()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var cancellation = new CancellationTokenSource();

        // Act
        var result = await sender.Send(new SettleInvoiceCommand(cancellation), cancellation.Token);

        // Assert: a token cancelled after the handler returned must not leave the transaction open.
        result.IsSuccess.Should().BeTrue();
        scope.ServiceProvider.GetRequiredService<FakeTransactionManager>().Calls.Should().Equal(
            FakeTransactionManager.Begin,
            FakeTransactionManager.Commit);
    }

    [Fact]
    public async Task Send_Query_DoesNotOpenTransaction()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        await sender.Send(new GetOrderStatusQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        scope.ServiceProvider.GetRequiredService<FakeTransactionManager>().Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_InvalidCommand_ShortCircuitsBeforeOpeningTransaction()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        await sender.Send(new RegisterCustomerCommand(string.Empty), TestContext.Current.CancellationToken);

        // Assert: Validation runs before Transaction in the fixed pipeline order, so an invalid command never opens one.
        scope.ServiceProvider.GetRequiredService<FakeTransactionManager>().Calls.Should().BeEmpty();
    }
}
