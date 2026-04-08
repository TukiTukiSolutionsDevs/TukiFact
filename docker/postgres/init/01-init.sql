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
-- to automatically apply RLS policies
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

-- Note: Call SELECT apply_rls_to_tenant_tables(); AFTER running EF Core migrations
