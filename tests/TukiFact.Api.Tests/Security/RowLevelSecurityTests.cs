using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TukiFact.Api.Tests.Fixtures;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Tests.Security;

/// <summary>
/// Verifies the PostgreSQL row level security layer that backs multi-tenant isolation.
/// Booting <see cref="TukiFactAppFactory"/> runs migrations and then
/// <c>SELECT apply_rls_to_tenant_tables();</c> exactly like Program.cs does in
/// production, so these tests observe the real catalog state after startup.
///
/// The raw-SQL test deliberately bypasses EF Core and application filters: it runs
/// under a non-owner, non-superuser role so that the policy itself is what hides the
/// other tenant's rows. Superusers and table owners bypass RLS, which is why the
/// container's default user cannot be used to prove the policy works.
/// </summary>
[Collection("Postgres")]
public class RowLevelSecurityTests : IAsyncLifetime
{
    private const string ProbeRole = "rls_probe";

    private readonly PostgresFixture _postgres;
    private TukiFactAppFactory _factory = null!;

    public RowLevelSecurityTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public Task InitializeAsync()
    {
        _factory = new TukiFactAppFactory(_postgres.ConnectionString);
        // Force host construction so migrations + apply_rls_to_tenant_tables() run.
        _ = _factory.Server;
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [Fact]
    public async Task Every_table_with_a_tenant_column_has_rls_enabled_and_an_isolation_policy()
    {
        await using var conn = new NpgsqlConnection(_postgres.ConnectionString);
        await conn.OpenAsync();

        const string sql = """
            SELECT c.table_name,
                   c.column_name,
                   cl.relrowsecurity,
                   EXISTS (
                       SELECT 1 FROM pg_policies p
                       WHERE p.schemaname = c.table_schema
                         AND p.tablename  = c.table_name
                         AND p.policyname LIKE 'tenant_isolation_%'
                   ) AS has_isolation_policy
            FROM information_schema.columns c
            JOIN pg_namespace n ON n.nspname = c.table_schema
            JOIN pg_class cl ON cl.relnamespace = n.oid AND cl.relname = c.table_name
            WHERE c.table_schema = 'public'
              AND cl.relkind = 'r'
              AND c.column_name IN ('TenantId', 'tenant_id')
              AND c.table_name <> 'tenants'
            ORDER BY c.table_name
            """;

        var rows = new List<(string Table, string Column, bool RlsEnabled, bool HasPolicy)>();
        await using (var cmd = new NpgsqlCommand(sql, conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                rows.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetBoolean(2),
                    reader.GetBoolean(3)));
            }
        }

        rows.Should().NotBeEmpty("the schema must contain tenant-scoped tables after migrations");

        var unprotected = rows
            .Where(r => !r.RlsEnabled || !r.HasPolicy)
            .Select(r => $"{r.Table}.{r.Column} (rls_enabled={r.RlsEnabled}, has_policy={r.HasPolicy})")
            .ToList();

        unprotected.Should().BeEmpty(
            "every tenant-scoped table must have row level security enabled and a tenant_isolation_* policy");
    }

    [Fact]
    public async Task Raw_sql_under_non_bypass_role_cannot_see_other_tenant_rows()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();

        // Arrange: two tenants, one customer each, inserted through EF as the DB owner
        // (no tenant context, so nothing is filtered at insert time).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Tenants.AddRange(
                NewTenant(tenantA),
                NewTenant(tenantB));
            db.Customers.AddRange(
                new Customer { Id = customerA, TenantId = tenantA, DocType = "1", DocNumber = "11111111", Name = "Customer A" },
                new Customer { Id = customerB, TenantId = tenantB, DocType = "1", DocNumber = "22222222", Name = "Customer B" });
            await db.SaveChangesAsync();
        }

        await using var conn = new NpgsqlConnection(_postgres.ConnectionString);
        await conn.OpenAsync();
        await EnsureProbeRoleAsync(conn);

        // Act: read the raw table as a role that is neither owner nor superuser, with the
        // tenant GUC bound to tenant A for the duration of the transaction.
        var visible = new List<Guid>();
        await using (var tx = await conn.BeginTransactionAsync())
        {
            await using (var setRole = new NpgsqlCommand($"SET LOCAL ROLE {ProbeRole}", conn, tx))
            {
                await setRole.ExecuteNonQueryAsync();
            }

            await using (var setTenant = new NpgsqlCommand("SELECT set_config('app.current_tenant', @tenant, true)", conn, tx))
            {
                setTenant.Parameters.AddWithValue("tenant", tenantA.ToString());
                await setTenant.ExecuteNonQueryAsync();
            }

            await using (var select = new NpgsqlCommand(
                "SELECT \"Id\" FROM customers WHERE \"Id\" = ANY(@ids)", conn, tx))
            {
                select.Parameters.AddWithValue("ids", new[] { customerA, customerB });
                await using var reader = await select.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    visible.Add(reader.GetGuid(0));
                }
            }

            await tx.RollbackAsync();
        }

        // Assert
        visible.Should().Contain(customerA, "the current tenant's own rows must remain visible");
        visible.Should().NotContain(customerB, "row level security must hide other tenants' rows even for raw SQL");
    }

    private static Tenant NewTenant(Guid id) => new()
    {
        Id = id,
        Ruc = "20" + Random.Shared.NextInt64(100_000_000, 999_999_999),
        RazonSocial = $"RLS Tenant {id:N}",
    };

    /// <summary>
    /// Creates a login-less role with plain SELECT rights. It owns nothing and has no
    /// BYPASSRLS, so any policy on the tables applies to it.
    /// </summary>
    private static async Task EnsureProbeRoleAsync(NpgsqlConnection conn)
    {
        var sql = $"""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{ProbeRole}') THEN
                    CREATE ROLE {ProbeRole} NOLOGIN;
                END IF;
            END
            $$;
            GRANT USAGE ON SCHEMA public TO {ProbeRole};
            GRANT SELECT ON ALL TABLES IN SCHEMA public TO {ProbeRole};
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
