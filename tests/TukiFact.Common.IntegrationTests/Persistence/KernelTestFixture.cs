using Dapper;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.Infrastructure.Modules;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using TukiFact.TestSupport.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>
/// The <see cref="KernelTestModule"/> on the shared kernel integration fixture. Grants the probe role and enables
/// row level security on <c>kernel_test.orders</c> with the same policy shape production tables use, plus the
/// <c>app.bypass_rls</c> clause the RLS hotfix follow-up will add repo-wide.
/// </summary>
public sealed class KernelTestFixture : KernelIntegrationFixture
{
    private const string ReadOrderSql = """
        SELECT id           AS Id,
               tenant_id    AS TenantId,
               reference    AS Reference,
               created_at   AS CreatedAt,
               created_by   AS CreatedBy,
               updated_at   AS UpdatedAt,
               updated_by   AS UpdatedBy,
               sync_version AS SyncVersion
        FROM kernel_test.orders
        WHERE id = @OrderId
        """;

    public MutableTimeProvider Clock { get; } = new();

    public DomainEventLog Events { get; } = new();

    public KernelLogSink Logs { get; } = new();

    protected override IModule Module { get; } = new KernelTestModule();

    protected override string Schema => KernelTestDbContext.SchemaName;

    public Task<Result> SendAsync(IRequest<Result> command, Guid? tenantId = null, Guid? userId = null) =>
        SendAsync(command, scope =>
        {
            var currentUser = scope.GetRequiredService<TestCurrentUser>();
            currentUser.TenantId = tenantId;
            currentUser.UserId = userId;
        });

    /// <summary>Reads through the EF-tracked owner connection (no RLS-bypass), used by tests that assert on the
    /// raw row after commit as a system-scope superuser check, distinct from the tenant-scoped Dapper path below.</summary>
    public async Task<KernelTestOrderRow?> ReadOrderAsync(Guid orderId, Guid? tenantId = null)
    {
        await using var scope = Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestCurrentUser>().TenantId = tenantId;
        var database = scope.ServiceProvider.GetRequiredService<IKernelTestOrderReadDbConnection>();
        var connection = await database.OpenAsync(TestContext.Current.CancellationToken);

        return await connection.QuerySingleOrDefaultAsync<KernelTestOrderRow>(ReadOrderSql, new { OrderId = orderId });
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddLogging(logging => logging.AddProvider(new KernelLogSinkProvider(Logs)));
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Clock));
        services.AddSingleton(Events);
        services.AddScoped<TestCurrentUser>();
        services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<TestCurrentUser>());
        services.AddScoped<IIntegrationEventPublisher, UnconfiguredIntegrationEventPublisher>();
    }

    protected override async Task ApplyRowLevelSecurityAsync(NpgsqlConnection ownerConnection)
    {
        var sql = $"""
            GRANT USAGE ON SCHEMA {Schema} TO {ProbeRoleName};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {Schema} TO {ProbeRoleName};

            ALTER TABLE {Schema}.orders ENABLE ROW LEVEL SECURITY;
            ALTER TABLE {Schema}.orders FORCE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS tenant_isolation_orders ON {Schema}.orders;
            CREATE POLICY tenant_isolation_orders ON {Schema}.orders
                FOR ALL
                USING (tenant_id = current_tenant_id() OR current_setting('app.bypass_rls', true) = 'on')
                WITH CHECK (tenant_id = current_tenant_id() OR current_setting('app.bypass_rls', true) = 'on');
            """;

        await using var command = new NpgsqlCommand(sql, ownerConnection);
        await command.ExecuteNonQueryAsync();
    }
}
