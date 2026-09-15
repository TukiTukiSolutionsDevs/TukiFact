-- TukiFact PostgreSQL initialization
-- This runs ONCE when the container is first created

-- Create the application database
-- (Docker POSTGRES_DB handles this, but we set up the config)

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create a function to get current tenant (used by RLS policies)
CREATE OR REPLACE FUNCTION current_tenant_id() RETURNS uuid AS $$
BEGIN
    RETURN current_setting('app.current_tenant', true)::uuid;
EXCEPTION
    WHEN OTHERS THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE;

-- Create a function that will be called after EF Core creates tables
-- to automatically apply RLS policies.
--
-- IMPORTANT: this body is duplicated verbatim in the EF migration
-- FixRlsPolicyDiscovery (src/TukiFact.Infrastructure/Persistence/Migrations).
-- Keep both in sync: init SQL only runs on brand-new databases, the migration is
-- what upgrades existing ones.
--
-- EF Core generates the tenant column as "TenantId" (quoted, mixed case) while
-- table names are lowercase via ToTable(). Discovery therefore matches both
-- spellings and the policy references the discovered column name, safely quoted.
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

-- Note: Call SELECT apply_rls_to_tenant_tables(); AFTER running EF Core migrations
