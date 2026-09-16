using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace TukiFact.Common.IntegrationTests.Persistence.Interceptors;

[Collection(KernelPostgresCollection.Name)]
public sealed class DomainEventsInterceptorTests(KernelTestFixture fixture)
{
    [Fact]
    public async Task CommitAsync_AggregateWithEvents_DispatchesEachEventOnceInOrderAndClearsThem()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await using var scope = fixture.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = tenantId;
        var transactions = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var order = KernelTestOrder.Register(orderId, tenantId, "draft", fixture.Clock.UtcNow);
        order.Relabel("final", fixture.Clock.UtcNow);
        await transactions.BeginTransactionAsync(TestContext.Current.CancellationToken);
        scope.ServiceProvider.GetRequiredService<IKernelTestOrderRepository>().Add(order);

        // Act
        await transactions.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        fixture.Events.For(orderId).Select(domainEvent => domainEvent.GetType()).Should().Equal(
            typeof(KernelTestOrderRegisteredDomainEvent),
            typeof(KernelTestOrderRelabeledDomainEvent));
        order.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_CalledAgainWithoutNewEvents_DoesNotDispatchTwice()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await using var scope = fixture.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = tenantId;
        var transactions = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        await transactions.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var context = scope.ServiceProvider.GetRequiredService<KernelTestDbContext>();
        context.Add(KernelTestOrder.Register(orderId, tenantId, "draft", fixture.Clock.UtcNow));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        fixture.Events.For(orderId).Should().ContainSingle();
        await transactions.RollbackAsync(TestContext.Current.CancellationToken);
    }
}
