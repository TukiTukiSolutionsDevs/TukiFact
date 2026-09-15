using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Migrations;

// Lets the API test suite assert this migration body stays identical to
// docker/postgres/init/01-init.sql (see RlsFunctionParityTests).
[assembly: InternalsVisibleTo("TukiFact.Api.Tests")]

#nullable disable

namespace TukiFact.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Security hotfix: apply_rls_to_tenant_tables() discovered tenant tables by the
    /// column name 'tenant_id', but EF Core generates the column as "TenantId", so the
    /// function matched zero tables and no row level security policy was ever created.
    ///
    /// docker/postgres/init/01-init.sql only runs on brand-new databases, so this
    /// migration ships the corrected function body to existing databases. The body
    /// below MUST stay byte-for-byte identical to the one in the init script.
    /// Program.cs calls SELECT apply_rls_to_tenant_tables() after MigrateAsync(), which
    /// is what actually enables RLS and (re)creates the policies on every boot.
    /// </summary>
    public partial class FixRlsPolicyDiscovery : Migration
    {
        private const string CurrentTenantIdFunction = """
            CREATE OR REPLACE FUNCTION current_tenant_id() RETURNS uuid AS $$
            BEGIN
                RETURN current_setting('app.current_tenant', true)::uuid;
            EXCEPTION
                WHEN OTHERS THEN
                    RETURN NULL;
            END;
            $$ LANGUAGE plpgsql STABLE;
            """;

        internal const string FixedApplyRlsFunction = """
            CREATE OR REPLACE FUNCTION apply_rls_to_tenant_tables() RETURNS void AS $$
            DECLARE
                rec RECORD;
            BEGIN
                -- Serializes concurrent app boots (several replicas call this at startup).
                PERFORM pg_advisory_xact_lock(hashtext('apply_rls_to_tenant_tables'));

                FOR rec IN
                    SELECT c.table_name, c.column_name
                    FROM information_schema.columns c
                    JOIN pg_namespace n ON n.nspname = c.table_schema
                    JOIN pg_class cl ON cl.relnamespace = n.oid AND cl.relname = c.table_name
                    WHERE c.table_schema = 'public'
                      AND cl.relkind = 'r'
                      AND c.column_name IN ('tenant_id', 'TenantId')
                      AND c.table_name <> 'tenants'  -- tenants table doesn't filter by itself
                    ORDER BY c.table_name
                LOOP
                    -- Enable RLS
                    EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', rec.table_name);

                    -- Drop existing policy if any
                    EXECUTE format(
                        'DROP POLICY IF EXISTS %I ON public.%I',
                        'tenant_isolation_' || rec.table_name, rec.table_name
                    );

                    -- Create isolation policy against the real column name
                    -- The policy uses plain equality, so rows with NULL TenantId (e.g. idempotency_keys.TenantId
                    -- is nullable) are invisible to non-bypass roles by design (fail-closed). Anonymous-row
                    -- semantics will be decided in the kernel RLS design.
                    EXECUTE format(
                        'CREATE POLICY %I ON public.%I
                         FOR ALL
                         USING (%I = current_tenant_id())
                         WITH CHECK (%I = current_tenant_id())',
                        'tenant_isolation_' || rec.table_name, rec.table_name,
                        rec.column_name, rec.column_name
                    );

                    RAISE NOTICE 'RLS applied to table: % (column: %)', rec.table_name, rec.column_name;
                END LOOP;
            END;
            $$ LANGUAGE plpgsql;
            """;

        /// <summary>
        /// Previous (broken) body, restored by Down(). It only matched a lowercase
        /// 'tenant_id' column, which no EF-generated table has.
        /// </summary>
        private const string PreviousApplyRlsFunction = """
            CREATE OR REPLACE FUNCTION apply_rls_to_tenant_tables() RETURNS void AS $$
            DECLARE
                tbl TEXT;
            BEGIN
                FOR tbl IN
                    SELECT table_name FROM information_schema.columns
                    WHERE column_name = 'tenant_id'
                    AND table_schema = 'public'
                    AND table_name != 'tenants'  -- tenants table doesn't filter by itself
                LOOP
                    -- Enable RLS
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', tbl);

                    -- Drop existing policy if any
                    EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_%I ON %I', tbl, tbl);

                    -- Create isolation policy
                    EXECUTE format(
                        'CREATE POLICY tenant_isolation_%I ON %I
                         FOR ALL
                         USING (tenant_id = current_tenant_id())
                         WITH CHECK (tenant_id = current_tenant_id())',
                        tbl, tbl
                    );

                    RAISE NOTICE 'RLS applied to table: %', tbl;
                END LOOP;
            END;
            $$ LANGUAGE plpgsql;
            """;

        /// <summary>
        /// Removes every policy the fixed function may have created and disables RLS
        /// again on those tables. Used by Down() so a rollback leaves the catalog as it
        /// was before this migration (no policies, RLS disabled).
        /// </summary>
        private const string DropTenantIsolationPolicies = """
            DO $$
            DECLARE
                rec RECORD;
            BEGIN
                FOR rec IN
                    SELECT schemaname, tablename, policyname
                    FROM pg_policies
                    WHERE schemaname = 'public'
                      AND policyname LIKE 'tenant_isolation_%'
                LOOP
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I.%I', rec.policyname, rec.schemaname, rec.tablename);
                    EXECUTE format('ALTER TABLE %I.%I DISABLE ROW LEVEL SECURITY', rec.schemaname, rec.tablename);
                END LOOP;
            END
            $$;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CurrentTenantIdFunction);
            migrationBuilder.Sql(FixedApplyRlsFunction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DropTenantIsolationPolicies);
            migrationBuilder.Sql(PreviousApplyRlsFunction);
        }
    }
}
