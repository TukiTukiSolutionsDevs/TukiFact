# 05 - Esquema de Base de Datos

## Estrategia Multi-Tenant
- **Enfoque**: Shared database, shared schema con Row Level Security (RLS)
- **Columna**: `tenant_id UUID NOT NULL` en CADA tabla de negocio
- **RLS Policy**: Filtra automáticamente por `current_setting('app.current_tenant')`
- **Ventaja**: Una sola base, fácil de mantener, aislamiento a nivel de fila

```sql
-- Habilitar RLS en una tabla
ALTER TABLE invoices ENABLE ROW LEVEL SECURITY;

-- Policy que filtra por tenant
CREATE POLICY tenant_isolation ON invoices
    USING (tenant_id = current_setting('app.current_tenant')::uuid);

-- El middleware .NET setea el tenant en cada request:
-- SET LOCAL app.current_tenant = 'uuid-del-tenant';
```

## Tablas Principales

### Módulo: Tenants y Auth

```sql
-- Empresas/Tenants
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ruc VARCHAR(11) NOT NULL UNIQUE,
    razon_social VARCHAR(200) NOT NULL,
    nombre_comercial VARCHAR(200),
    direccion TEXT,
    ubigeo VARCHAR(6),
    departamento VARCHAR(50),
    provincia VARCHAR(50),
    distrito VARCHAR(50),
    logo_url TEXT,
    primary_color VARCHAR(7) DEFAULT '#1a73e8',
    plan_id UUID REFERENCES plans(id),
    certificate_data BYTEA, -- cert encriptado
    certificate_password_encrypted TEXT,
    certificate_expires_at TIMESTAMPTZ,
    sunat_user VARCHAR(20), -- SOL user
    sunat_password_encrypted TEXT, -- SOL password encriptado
    environment VARCHAR(10) DEFAULT 'beta', -- beta | production
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT now(),
    updated_at TIMESTAMPTZ DEFAULT now()
);

-- Usuarios
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    email VARCHAR(255) NOT NULL,
    password_hash TEXT NOT NULL,
    full_name VARCHAR(200),
    role VARCHAR(20) NOT NULL DEFAULT 'emisor', -- admin, emisor, consulta
    is_active BOOLEAN DEFAULT true,
    last_login_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT now(),
    UNIQUE(tenant_id, email)
);

-- API Keys
CREATE TABLE api_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    key_hash TEXT NOT NULL UNIQUE, -- hash del API key
    key_prefix VARCHAR(8) NOT NULL, -- primeros 8 chars para identificación
    name VARCHAR(100),
    permissions JSONB DEFAULT '["emit","query"]',
    is_active BOOLEAN DEFAULT true,
    last_used_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT now()
);

-- Planes de suscripción
CREATE TABLE plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL UNIQUE,
    price_monthly DECIMAL(10,2) NOT NULL,
    max_documents_per_month INT NOT NULL,
    features JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT now()
);
```

### Módulo: Series y Numeración

```sql
-- Series por tenant y punto de emisión
CREATE TABLE series (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    document_type VARCHAR(2) NOT NULL, -- 01, 03, 07, 08, RC, RA
    serie VARCHAR(4) NOT NULL, -- F001, B001, etc.
    current_correlative BIGINT NOT NULL DEFAULT 0,
    emission_point VARCHAR(50) DEFAULT 'PRINCIPAL',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT now(),
    UNIQUE(tenant_id, document_type, serie)
);
```

### Módulo: Documentos Emitidos

```sql
-- Documentos (facturas, boletas, notas)
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    document_type VARCHAR(2) NOT NULL, -- 01, 03, 07, 08
    serie VARCHAR(4) NOT NULL,
    correlative BIGINT NOT NULL,
    full_number VARCHAR(13) NOT NULL, -- F001-00000001
    
    -- Tipo de operación (Cat. 51)
    operation_type VARCHAR(4) NOT NULL DEFAULT '0101',
    
    -- Emisor (denormalizado para performance)
    emisor_ruc VARCHAR(11) NOT NULL,
    emisor_razon_social VARCHAR(200) NOT NULL,
    
    -- Receptor
    receptor_doc_type VARCHAR(1) NOT NULL, -- Cat. 06
    receptor_doc_number VARCHAR(15) NOT NULL,
    receptor_razon_social VARCHAR(200) NOT NULL,
    receptor_direccion TEXT,
    
    -- Moneda y fechas
    currency VARCHAR(3) NOT NULL DEFAULT 'PEN', -- Cat. 02
    exchange_rate DECIMAL(10,6),
    issue_date DATE NOT NULL,
    issue_time TIME NOT NULL,
    due_date DATE,
    
    -- Totales
    total_gravado DECIMAL(15,2) DEFAULT 0,
    total_exonerado DECIMAL(15,2) DEFAULT 0,
    total_inafecto DECIMAL(15,2) DEFAULT 0,
    total_gratuito DECIMAL(15,2) DEFAULT 0,
    total_exportacion DECIMAL(15,2) DEFAULT 0,
    total_igv DECIMAL(15,2) DEFAULT 0,
    total_isc DECIMAL(15,2) DEFAULT 0,
    total_icbper DECIMAL(15,2) DEFAULT 0,
    total_otros_tributos DECIMAL(15,2) DEFAULT 0,
    total_descuentos DECIMAL(15,2) DEFAULT 0,
    total_cargos DECIMAL(15,2) DEFAULT 0,
    total_anticipos DECIMAL(15,2) DEFAULT 0,
    total_venta DECIMAL(15,2) NOT NULL,
    
    -- Notas de crédito/débito
    ref_document_type VARCHAR(2), -- tipo del doc referenciado
    ref_document_number VARCHAR(13), -- número del doc referenciado
    credit_note_type VARCHAR(2), -- Cat. 09
    debit_note_type VARCHAR(2), -- Cat. 10
    note_description TEXT,
    
    -- Leyendas
    legends JSONB DEFAULT '[]', -- [{code, value}]
    
    -- Estado SUNAT
    sunat_status VARCHAR(20) DEFAULT 'pending',
    -- pending, sent, accepted, rejected, observed, voided
    sunat_response_code VARCHAR(10),
    sunat_response_description TEXT,
    sunat_observations JSONB DEFAULT '[]',
    cdr_received_at TIMESTAMPTZ,
    
    -- Resumen diario (para boletas)
    summary_id UUID REFERENCES daily_summaries(id),
    
    -- Storage references
    xml_storage_key TEXT, -- MinIO key
    pdf_storage_key TEXT,
    cdr_storage_key TEXT,
    xml_hash VARCHAR(64), -- SHA256 del XML
    
    -- Metadata
    external_id VARCHAR(100), -- ID del sistema externo
    metadata JSONB DEFAULT '{}',
    
    created_at TIMESTAMPTZ DEFAULT now(),
    updated_at TIMESTAMPTZ DEFAULT now(),
    
    UNIQUE(tenant_id, document_type, full_number)
);

CREATE INDEX idx_documents_tenant_date ON documents(tenant_id, issue_date DESC);
CREATE INDEX idx_documents_tenant_status ON documents(tenant_id, sunat_status);
CREATE INDEX idx_documents_receptor ON documents(tenant_id, receptor_doc_number);
CREATE INDEX idx_documents_external_id ON documents(tenant_id, external_id);

-- Detalle de ítems del documento
CREATE TABLE document_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    item_order INT NOT NULL,
    
    -- Producto
    product_code VARCHAR(30),
    sunat_product_code VARCHAR(20), -- Cat. 25
    description TEXT NOT NULL,
    unit_code VARCHAR(5) NOT NULL DEFAULT 'NIU', -- Cat. 03
    
    -- Cantidades y precios
    quantity DECIMAL(15,4) NOT NULL,
    unit_price DECIMAL(15,4) NOT NULL, -- sin IGV
    unit_price_with_tax DECIMAL(15,4), -- con IGV
    
    -- Afectación tributaria
    igv_type VARCHAR(2) NOT NULL DEFAULT '10', -- Cat. 07
    igv_percentage DECIMAL(5,2) DEFAULT 18.00,
    igv_amount DECIMAL(15,2) DEFAULT 0,
    
    isc_type VARCHAR(2), -- Cat. 08
    isc_amount DECIMAL(15,2) DEFAULT 0,
    
    icbper_amount DECIMAL(15,2) DEFAULT 0,
    icbper_quantity INT DEFAULT 0, -- cantidad de bolsas
    
    -- Descuentos por ítem
    discount_type VARCHAR(2), -- Cat. 53
    discount_amount DECIMAL(15,2) DEFAULT 0,
    discount_percentage DECIMAL(5,2),
    
    -- Totales del ítem
    taxable_amount DECIMAL(15,2) NOT NULL, -- base imponible
    total_tax DECIMAL(15,2) DEFAULT 0,
    line_total DECIMAL(15,2) NOT NULL, -- valor de venta
    
    -- Propiedades adicionales
    additional_properties JSONB DEFAULT '[]',
    
    created_at TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX idx_items_document ON document_items(document_id);
```

### Módulo: Resúmenes y Bajas

```sql
-- Resúmenes diarios de boletas
CREATE TABLE daily_summaries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    summary_number VARCHAR(20) NOT NULL, -- RC-YYYYMMDD-NNNNN
    reference_date DATE NOT NULL,
    generation_date DATE NOT NULL,
    
    total_documents INT NOT NULL DEFAULT 0,
    
    -- Estado SUNAT
    sunat_status VARCHAR(20) DEFAULT 'pending',
    sunat_ticket VARCHAR(50), -- ticket de SUNAT
    sunat_response_code VARCHAR(10),
    sunat_response_description TEXT,
    
    xml_storage_key TEXT,
    cdr_storage_key TEXT,
    
    created_at TIMESTAMPTZ DEFAULT now(),
    updated_at TIMESTAMPTZ DEFAULT now()
);

-- Comunicaciones de baja
CREATE TABLE void_communications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    communication_number VARCHAR(20) NOT NULL, -- RA-YYYYMMDD-NNNNN
    generation_date DATE NOT NULL,
    void_reason TEXT NOT NULL,
    
    -- Documentos anulados
    voided_documents JSONB NOT NULL, -- [{type, serie, correlative, reason}]
    
    -- Estado SUNAT
    sunat_status VARCHAR(20) DEFAULT 'pending',
    sunat_ticket VARCHAR(50),
    sunat_response_code VARCHAR(10),
    sunat_response_description TEXT,
    
    xml_storage_key TEXT,
    cdr_storage_key TEXT,
    
    created_at TIMESTAMPTZ DEFAULT now(),
    updated_at TIMESTAMPTZ DEFAULT now()
);
```

### Módulo: Catálogos

```sql
-- Catálogos SUNAT
CREATE TABLE catalogs (
    id SERIAL PRIMARY KEY,
    catalog_number VARCHAR(5) NOT NULL,
    code VARCHAR(20) NOT NULL,
    description TEXT NOT NULL,
    parent_code VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    version INT DEFAULT 1,
    valid_from DATE,
    valid_until DATE,
    metadata JSONB DEFAULT '{}',
    UNIQUE(catalog_number, code, version)
);

CREATE INDEX idx_catalogs_number ON catalogs(catalog_number, is_active);
```

### Módulo: Webhooks y Eventos

```sql
-- Configuración de webhooks por tenant
CREATE TABLE webhook_configs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    url TEXT NOT NULL,
    secret VARCHAR(64), -- para firmar payloads
    events TEXT[] DEFAULT ARRAY['cdr.accepted','cdr.rejected','cdr.observed'],
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT now()
);

-- Log de intentos de webhook
CREATE TABLE webhook_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    webhook_config_id UUID REFERENCES webhook_configs(id),
    event_type VARCHAR(50) NOT NULL,
    payload JSONB NOT NULL,
    response_status INT,
    response_body TEXT,
    attempt INT DEFAULT 1,
    success BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT now()
);
```

### Módulo: Auditoría

```sql
-- Log de auditoría
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    user_id UUID,
    action VARCHAR(50) NOT NULL, -- emit, void, query, config_change
    entity_type VARCHAR(30) NOT NULL, -- document, tenant, series, user
    entity_id UUID,
    details JSONB DEFAULT '{}',
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX idx_audit_tenant_date ON audit_logs(tenant_id, created_at DESC);
```

### Módulo: Uso y Metering

```sql
-- Conteo de documentos por mes para billing
CREATE TABLE usage_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    period VARCHAR(7) NOT NULL, -- YYYY-MM
    document_count INT DEFAULT 0,
    api_calls INT DEFAULT 0,
    ai_tokens_used INT DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT now(),
    updated_at TIMESTAMPTZ DEFAULT now(),
    UNIQUE(tenant_id, period)
);
```

## Aplicar RLS a Todas las Tablas

```sql
-- Script para aplicar RLS a todas las tablas con tenant_id
DO $$
DECLARE
    tbl TEXT;
BEGIN
    FOR tbl IN
        SELECT table_name FROM information_schema.columns
        WHERE column_name = 'tenant_id'
        AND table_schema = 'public'
    LOOP
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', tbl);
        EXECUTE format(
            'CREATE POLICY tenant_isolation_%I ON %I
             USING (tenant_id = current_setting(''app.current_tenant'')::uuid)',
            tbl, tbl
        );
    END LOOP;
END $$;
```
