using Dapper;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Infrastructure.Persistence;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>Threat matrix (design §10): tenant isolation on the Dapper read path and session-GUC pool bleed.</summary>
[Collection(KernelPostgresCollection.Name)]
public sealed class ReadDbConnectionTests(KernelTestFixture fixture)
{
    [Fact]
    public async Task OpenAsync_QueryAsOtherTenant_HidesRow()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, tenantA, "dapper-order"), tenantA);

        // Act
        var visibleToOwner = await fixture.ReadOrderAsync(orderId, tenantA);
        var visibleToOther = await fixture.ReadOrderAsync(orderId, tenantB);

        // Assert
        visibleToOwner.Should().NotBeNull();
        visibleToOwner!.Reference.Should().Be("dapper-order");
        visibleToOther.Should().BeNull("row level security must hide the row from a different tenant on the Dapper path too");
    }

    [Fact]
    public async Task DisposeAsync_ScopeDisposed_LeavesNoResidualTenantInPool()
    {
        // Arrange: exhaust the read path as tenant A through the DI-scoped wrapper, then dispose that scope so
        // Dispose resets the GUC before the physical connection returns to the Npgsql pool.
        var tenantA = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = tenantA;
            var database = scope.ServiceProvider.GetRequiredService<IKernelTestOrderReadDbConnection>();
            await database.OpenAsync(cancellationToken);
        }

        // Act: open a bare connection from the same factory (same connection string, same pool key) and read the
        // GUC directly, bypassing ReadDbConnection's own set_config call, so a residual value cannot be masked.
        var connectionFactory = fixture.Services.GetRequiredService<ISqlConnectionFactory>();
        await using var rawConnection = (NpgsqlConnection)connectionFactory.CreateConnection();
        await rawConnection.OpenAsync(cancellationToken);
        var residualTenant = await rawConnection.QuerySingleAsync<string?>("SELECT current_setting('app.current_tenant', true)");

        // Assert
        residualTenant.Should().BeNullOrEmpty("a reused pooled connection must never carry a previous tenant's GUC");
    }

    [Fact]
    public async Task OpenAsync_NoTenantAndNoSystemScope_FailsClosedAndLogsWarning()
    {
        // Arrange
        var warningsBefore = fixture.Logs.Count(LogLevel.Warning, "Tenant context missing");
        await using var scope = fixture.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IKernelTestOrderReadDbConnection>();

        // Act
        var act = () => database.OpenAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<TenantContextMissingException>();
        fixture.Logs.Count(LogLevel.Warning, "Tenant context missing").Should().Be(warningsBefore + 1);
    }
}
