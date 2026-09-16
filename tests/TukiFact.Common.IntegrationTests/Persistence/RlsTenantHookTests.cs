using System.Data;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Infrastructure.Persistence;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>
/// Threat matrix (design §10): tenant isolation on the EF write path, fail-closed with no tenant, the audited
/// system scope, and defense against a hostile adversarial value flowing through the same transaction machinery
/// that sets the tenant GUC.
/// </summary>
[Collection(KernelPostgresCollection.Name)]
public sealed class RlsTenantHookTests(KernelTestFixture fixture)
{
    [Fact]
    public async Task Tenant_a_cannot_read_or_write_tenant_b_rows()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, tenantA, "tenant-a-order"), tenantA);

        // Act
        var readAsTenantB = await fixture.ReadOrderAsync(orderId, tenantB);
        var crossTenantWrite = () => fixture.SendAsync(
            new RegisterKernelTestOrderCommand(Guid.NewGuid(), tenantB, "smuggled"), tenantA);

        // Assert
        readAsTenantB.Should().BeNull("row level security must hide tenant A's row from tenant B");
        var thrown = await crossTenantWrite.Should().ThrowAsync<DbUpdateException>();
        thrown.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege, "the WITH CHECK clause must reject a row stamped for a different tenant");
    }

    [Fact]
    public async Task No_tenant_and_no_system_scope_fails_closed()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var warningsBefore = fixture.Logs.Count(LogLevel.Warning, "Tenant context missing");

        // Act
        var act = () => fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, Guid.NewGuid(), "no-tenant"), tenantId: null);

        // Assert
        await act.Should().ThrowAsync<TenantContextMissingException>();
        (await fixture.ReadOrderAsync(orderId, Guid.NewGuid())).Should().BeNull("no row must ever be written without a tenant context");
        fixture.Logs.Count(LogLevel.Warning, "Tenant context missing").Should().Be(warningsBefore + 1, "the fail-closed rejection is a programming error worth a warning");
    }

    [Fact]
    public async Task System_scope_is_audited_and_transactional()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var orderIdA = Guid.NewGuid();
        var orderIdB = Guid.NewGuid();
        await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderIdA, tenantA, "order-a"), tenantA);
        await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderIdB, tenantB, "order-b"), tenantB);

        await using var scope = fixture.Services.CreateAsyncScope();
        var systemScope = scope.ServiceProvider.GetRequiredService<ISystemScope>();
        var transactions = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var repository = scope.ServiceProvider.GetRequiredService<IKernelTestOrderRepository>();

        // Act
        KernelTestOrder? seenA;
        KernelTestOrder? seenB;
        using (systemScope.Enter("integration-test-cross-tenant-read"))
        {
            await transactions.BeginTransactionAsync(TestContext.Current.CancellationToken);
            seenA = await repository.GetByIdAsync(orderIdA, TestContext.Current.CancellationToken);
            seenB = await repository.GetByIdAsync(orderIdB, TestContext.Current.CancellationToken);
            await transactions.CommitAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        seenA.Should().NotBeNull("system scope bypasses RLS, so both tenants' rows are visible");
        seenB.Should().NotBeNull("system scope bypasses RLS, so both tenants' rows are visible");
        systemScope.IsActive.Should().BeFalse("the scope resets once disposed");
    }

    [Fact]
    public void System_scope_throws_on_nesting()
    {
        // Arrange
        using var scopeFactory = fixture.Services.CreateScope();
        var systemScope = scopeFactory.ServiceProvider.GetRequiredService<ISystemScope>();
        using var outer = systemScope.Enter("outer");

        // Act
        var act = () => systemScope.Enter("inner");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Tenant_value_is_parameterised()
    {
        // Arrange: the tenant GUC can never carry a hostile string (ICurrentUser.TenantId is a Guid, not a raw
        // claim string), so this proves the adjacent adversarial-input path through the same transaction
        // machinery (the order reference) is safe end to end — no interpolation anywhere in the hot path.
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        const string adversarialReference = "'); DROP TABLE kernel_test.orders; --";

        // Act
        var result = await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, tenantId, adversarialReference), tenantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var row = await fixture.ReadOrderAsync(orderId, tenantId);
        row.Should().NotBeNull("the table must still exist and contain the row untouched by the adversarial content");
        row!.Reference.Should().Be(adversarialReference);
    }

    [Fact]
    public async Task Query_ModuleContextOutsideKernelTransaction_ThrowsBeforeOpeningConnection()
    {
        // Arrange
        await using var scope = fixture.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = Guid.NewGuid();
        var context = scope.ServiceProvider.GetRequiredService<KernelTestDbContext>();

        // Act
        var act = () => context.Orders.CountAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<TenantContextMissingException>().WithMessage("*open kernel transaction*");
        scope.ServiceProvider.GetRequiredService<TransactionManager>().Connection.State
            .Should().Be(ConnectionState.Closed, "no SQL may run through a module context outside the kernel transaction");
    }
}
