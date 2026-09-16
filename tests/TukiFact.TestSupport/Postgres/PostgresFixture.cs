using TukiFact.TestSupport.Containers;
using Testcontainers.PostgreSql;
using Xunit;

namespace TukiFact.TestSupport.Postgres;

/// <summary>
/// One Postgres container per xUnit collection, owner-privileged (superuser, table owner). Mounts the repo's
/// <c>docker/postgres/init/01-init.sql</c> so <c>current_tenant_id()</c> exists before any kernel module runs —
/// the same approach <c>TukiFact.Api.Tests.Fixtures.PostgresFixture</c> uses for the legacy RLS tests.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private static readonly string InitScriptPath = LocateInitScript();

    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder(ContainerImages.Postgres)
        .WithDatabase("tukifact_kernel_test")
        .WithUsername("tukifact_kernel_test")
        .WithPassword("tukifact_kernel_test_2026")
        .WithBindMount(InitScriptPath, "/docker-entrypoint-initdb.d/01-init.sql")
        .WithCleanUp(true)
        .Build();

    /// <summary>Owner (superuser) connection string. Only used for schema setup and probe-role provisioning.</summary>
    public string ConnectionString => Container.GetConnectionString();

    public async ValueTask InitializeAsync() => await Container.StartAsync();

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();

    private static string LocateInitScript()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "docker", "postgres", "init", "01-init.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException($"Could not locate docker/postgres/init/01-init.sql from {AppContext.BaseDirectory}.");
    }
}
