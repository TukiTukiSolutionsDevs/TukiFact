-- Sprint 12: Performance Indexes
-- Run manually or via EF migration

-- Documents: common query patterns
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_documents_tenant_status_date
    ON documents ("TenantId", "Status", "IssueDate" DESC);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_documents_tenant_customer
    ON documents ("TenantId", "CustomerDocNumber");

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_documents_fullnumber
    ON documents ("TenantId", "Serie", "Correlative" DESC);

-- Audit logs: time-based queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_tenant_action_time
    ON audit_logs ("TenantId", "Action", "CreatedAt" DESC);

-- Webhook deliveries: cleanup queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_webhook_deliveries_status
    ON webhook_deliveries ("Status", "CreatedAt");

-- Refresh tokens: cleanup expired
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_refresh_tokens_expires
    ON refresh_tokens ("ExpiresAt") WHERE "RevokedAt" IS NULL;

-- Analyze tables for query planner
ANALYZE documents;
ANALYZE document_items;
ANALYZE audit_logs;
ANALYZE users;
ANALYZE tenants;
