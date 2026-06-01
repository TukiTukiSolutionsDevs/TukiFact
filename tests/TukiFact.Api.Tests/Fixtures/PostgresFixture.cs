using Testcontainers.PostgreSql;

namespace TukiFact.Api.Tests.Fixtures;

/// <summary>
/// Spins up a real Postgres container once per test collection. The repo's
/// docker/postgres/init/*.sql is mounted into /docker-entrypoint-initdb.d so RLS
/// helper functions (apply_rls_to_tenant_tables, current_tenant_id) exist before
/// the API runs migrations.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private static readonly string InitScriptPath = LocateInitScript();

    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("tukifact_test")
        .WithUsername("tukifact_test")
        .WithPassword("tukifact_test_2026")
        .WithBindMount(InitScriptPath, "/docker-entrypoint-initdb.d/01-init.sql")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString => Container.GetConnectionString() + ";Include Error Detail=true";

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();

    private static string LocateInitScript()
    {
        // Walk up from the test binary directory until we find docker/postgres/init/01-init.sql.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "docker", "postgres", "init", "01-init.sql");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            "Could not locate docker/postgres/init/01-init.sql from " + AppContext.BaseDirectory);
    }
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
