using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Infrastructure.Persistence;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>TransactionBehavior + the EF transaction manager through the real mediator.</summary>
[Collection(KernelPostgresCollection.Name)]
public sealed class TransactionManagerTests(KernelTestFixture fixture)
{
    [Fact]
    public async Task Send_SuccessfulCommand_CommitsAggregate()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        var result = await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, tenantId, "fragile"), tenantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var row = await fixture.ReadOrderAsync(orderId, tenantId);
        row.Should().NotBeNull();
        row!.Reference.Should().Be("fragile");
    }

    [Fact]
    public async Task Send_CommandReturnsFailure_RollsBackWithoutDispatchingEvents()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        var result = await fixture.SendAsync(new RegisterKernelTestOrderThenFailCommand(orderId, tenantId), tenantId);

        // Assert
        result.Error.Should().Be(RegisterKernelTestOrderThenFailCommandHandler.Rejected);
        (await fixture.ReadOrderAsync(orderId, tenantId)).Should().BeNull();
        fixture.Events.For(orderId).Should().BeEmpty();
    }

    [Fact]
    public async Task Send_CommandThrows_RollsBackAggregate()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        var act = () => fixture.SendAsync(new RegisterKernelTestOrderThenCrashCommand(orderId, tenantId), tenantId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        (await fixture.ReadOrderAsync(orderId, tenantId)).Should().BeNull();
    }

    [Fact]
    public async Task Send_DomainEventHandlerThrows_RollsBackAggregate()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var command = new RegisterKernelTestOrderCommand(orderId, tenantId, KernelTestOrderRegisteredDomainEventHandler.RejectedReference);

        // Act
        var act = () => fixture.SendAsync(command, tenantId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Event handler rejected the order.");
        (await fixture.ReadOrderAsync(orderId, tenantId)).Should().BeNull("domain event handlers run inside the command transaction");
    }

    [Fact]
    public async Task BeginTransactionAsync_CalledTwiceInSameScope_ThrowsOnSecondCall()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var scope = fixture.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = tenantId;
        var transactions = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        await transactions.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => transactions.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await transactions.RollbackAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CommitAsync_ParticipantAfterCommitThrows_KeepsCommitNotifiesOthersAndLogs()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;
        var faulty = new ThrowingAfterCommitParticipant();
        var witness = new RecordingTransactionParticipant();
        await using var scope = fixture.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = tenantId;
        var transactions = scope.ServiceProvider.GetRequiredService<TransactionManager>();
        await transactions.BeginTransactionAsync(cancellationToken);
        scope.ServiceProvider.GetRequiredService<IKernelTestOrderRepository>()
            .Add(KernelTestOrder.Register(orderId, tenantId, "durable", fixture.Clock.UtcNow));
        transactions.Enlist(faulty);
        transactions.Enlist(witness);

        // Act
        await transactions.CommitAsync(cancellationToken);

        // Assert
        (await fixture.ReadOrderAsync(orderId, tenantId)).Should().NotBeNull("the commit is durable before any participant runs");
        witness.AfterCommitCalls.Should().Be(1, "one faulty participant must not starve the others");
        fixture.Logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Error && entry.Exception == faulty.Failure);
    }

    [Fact]
    public async Task CommitAsync_SaveChangesFailsAndRollbackParticipantThrows_SurfacesOriginalDbUpdateException()
    {
        // Arrange: the RLS WITH CHECK clause rejects a row stamped for another tenant, so SaveChanges throws while
        // the connection itself stays healthy.
        var tenantId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;
        var faulty = new ThrowingAfterRollbackParticipant();
        await using var scope = fixture.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = tenantId;
        var transactions = scope.ServiceProvider.GetRequiredService<TransactionManager>();
        await transactions.BeginTransactionAsync(cancellationToken);
        scope.ServiceProvider.GetRequiredService<IKernelTestOrderRepository>()
            .Add(KernelTestOrder.Register(Guid.NewGuid(), Guid.NewGuid(), "smuggled", fixture.Clock.UtcNow));
        transactions.Enlist(faulty);

        // Act
        var act = () => transactions.CommitAsync(cancellationToken);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>("a participant failure during rollback must be logged, not surfaced");
        transactions.CurrentTransaction.Should().BeNull("the transaction was rolled back and released");
        fixture.Logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Error && entry.Exception == faulty.Failure);
    }
}
