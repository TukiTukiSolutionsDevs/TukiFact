using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TukiFact.Common.IntegrationTests.Persistence.Interceptors;

[Collection(KernelPostgresCollection.Name)]
public sealed class SyncVersionInterceptorTests(KernelTestFixture fixture)
{
    [Fact]
    public async Task SaveChanges_NewAggregate_StartsSyncVersionAtOne()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, tenantId, "draft"), tenantId);

        // Assert
        (await fixture.ReadOrderAsync(orderId, tenantId))!.SyncVersion.Should().Be(1);
    }

    [Fact]
    public async Task SaveChanges_ModifiedAggregate_IncrementsSyncVersion()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, tenantId, "draft"), tenantId);

        // Act
        await fixture.SendAsync(new RelabelKernelTestOrderCommand(orderId, "final"), tenantId);

        // Assert
        (await fixture.ReadOrderAsync(orderId, tenantId))!.SyncVersion.Should().Be(2);
    }

    [Fact]
    public async Task CommitAsync_StaleSyncVersion_ThrowsConcurrencyConflictAndKeepsWinningWrite()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, tenantId, "draft"), tenantId);

        await using var winnerScope = fixture.Services.CreateAsyncScope();
        await using var staleScope = fixture.Services.CreateAsyncScope();
        winnerScope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = tenantId;
        staleScope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = tenantId;
        var winnerTransactions = winnerScope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var staleTransactions = staleScope.ServiceProvider.GetRequiredService<ITransactionManager>();

        await winnerTransactions.BeginTransactionAsync(cancellationToken);
        await staleTransactions.BeginTransactionAsync(cancellationToken);
        var winnerCopy = await winnerScope.ServiceProvider.GetRequiredService<IKernelTestOrderRepository>().GetByIdAsync(orderId, cancellationToken);
        var staleCopy = await staleScope.ServiceProvider.GetRequiredService<IKernelTestOrderRepository>().GetByIdAsync(orderId, cancellationToken);
        winnerCopy!.Relabel("winner", fixture.Clock.UtcNow);
        staleCopy!.Relabel("stale", fixture.Clock.UtcNow);
        await winnerTransactions.CommitAsync(cancellationToken);

        // Act
        var act = () => staleTransactions.CommitAsync(cancellationToken);

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        var row = await fixture.ReadOrderAsync(orderId, tenantId);
        row!.Reference.Should().Be("winner");
        row.SyncVersion.Should().Be(2);
    }
}
