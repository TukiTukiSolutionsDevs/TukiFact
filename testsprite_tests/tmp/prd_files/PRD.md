# TukiFact — Product Requirements Document (PRD)

> **Product**: TukiFact — Plataforma SaaS de Facturación Electrónica para Perú
> **Company**: Tuki Tuki Solutions S.A.C. | RUC: 20613614509
> **Version**: 1.0.0 | **Date**: 2026-04-08
> **Domain**: tukifact.net.pe

---

## 1. Product Overview

TukiFact is a multi-tenant SaaS platform for electronic invoicing in Peru, compliant with SUNAT (Peru's tax authority) regulations. It enables businesses to emit, sign, and send electronic documents (Facturas, Boletas, Notas de Crédito, Notas de Débito) via UBL 2.1 XML format, manage their invoicing lifecycle, and integrate with external systems via API and SDKs.

### Target Users
- **Small/Medium businesses** in Peru that need to comply with SUNAT electronic invoicing
- **Developers** building integrations with invoicing systems
- **Accountants** needing read access to invoicing data and reports

### Tech Stack
- **Backend API**: .NET 10 (ASP.NET Core) — `http://localhost:5100`
- **Frontend**: Next.js 16 (App Router, TypeScript, Tailwind, shadcn/ui) — `http://localhost:3000`
- **AI Service**: Python FastAPI — `http://localhost:8090`
- **Database**: PostgreSQL 18 with Row Level Security
- **Storage**: MinIO (S3-compatible) — `http://localhost:9000`
- **Messaging**: NATS JetStream — `nats://localhost:4222`

---

## 2. User Roles & Permissions

| Role | Can View | Can Emit | Can Manage Users | Can Manage Settings | Can Void |
|------|----------|----------|-----------------|--------------------| ---------|
| **admin** | Everything | Yes | Yes | Yes | Yes |
| **emisor** | Dashboard, Documents, Series | Yes | No | No | No |
| **consulta** | Dashboard, Documents, Reports | No | No | No | No |

---

## 3. Authentication & Authorization

### US-001: User Registration (Tenant Onboarding)
**As a** new business owner,
**I want to** register my company with my RUC,
**So that** I can start emitting electronic invoices.

**Acceptance Criteria:**
- [ ] User provides: RUC (11 digits), Razón Social, Nombre Comercial (optional), Dirección, admin email, admin password, admin full name
- [ ] RUC must be exactly 11 digits and start with 10 or 20
- [ ] RUC must be unique — duplicate registration returns HTTP 409
- [ ] On success: creates tenant + admin user + assigns Free plan
- [ ] Returns JWT access token + refresh token
- [ ] Redirects to `/dashboard` after registration

**Endpoint**: `POST /v1/auth/register`
**Frontend**: `/register`

### US-002: User Login
**As a** registered user,
**I want to** log in with my credentials,
**So that** I can access the platform.

**Acceptance Criteria:**
- [ ] User provides: email, password, tenant ID (UUID)
- [ ] Wrong credentials return HTTP 401 with message "Credenciales inválidas"
- [ ] Inactive user returns HTTP 401 with message "Usuario desactivado"
- [ ] On success: returns JWT access token (60 min) + refresh token (7 days)
- [ ] JWT contains: sub (userId), email, tenant_id, role
- [ ] After login, redirects to `/dashboard`

**Endpoint**: `POST /v1/auth/login`
**Frontend**: `/login`

### US-003: Token Refresh
**As an** authenticated user,
**I want** my session to auto-refresh,
**So that** I don't get logged out while working.

**Acceptance Criteria:**
- [ ] Client sends refresh token to get new access + refresh tokens
- [ ] Old refresh token is revoked after use (rotation)
- [ ] Expired/revoked refresh token returns HTTP 401
- [ ] Frontend API client auto-refreshes on 401 response

**Endpoint**: `POST /v1/auth/refresh`

### US-004: View Current User Info
**As an** authenticated user,
**I want to** see my account info,
**So that** I can verify my role and tenant.

**Acceptance Criteria:**
- [ ] Returns: userId, tenantId, email, role
- [ ] Requires valid JWT

**Endpoint**: `GET /v1/auth/me`

---

## 4. Document Emission (Core Feature)

### US-010: Emit Invoice (Factura)
**As an** emisor or admin,
**I want to** emit an electronic invoice,
**So that** it's sent to SUNAT and I get an accepted document.

**Acceptance Criteria:**
- [ ] Provide: documentType=01, serie (F-prefixed), customer data (RUC required for facturas), items with description/quantity/unitPrice/igvType
- [ ] System auto-assigns next correlative (atomic, no gaps)
- [ ] IGV (18%) is calculated automatically for gravado items
- [ ] UBL 2.1 XML is generated with correct SUNAT namespaces
- [ ] If tenant has certificate: XML is digitally signed (XMLDSig), hash code is stored
- [ ] XML is stored in MinIO bucket `tukifact-xml`
- [ ] Document is sent to SUNAT (beta stub accepts with code 0)
- [ ] QR data is generated in SUNAT format
- [ ] Returns DocumentResponse with all fields including status=accepted
- [ ] Factura requires RUC (doc type 6) for the customer — DNI should fail validation

**Endpoint**: `POST /v1/documents`
**Frontend**: `/documents/new`

### US-011: Emit Boleta
**As an** emisor,
**I want to** emit a boleta for individual customers,
**So that** I comply with SUNAT for B2C sales.

**Acceptance Criteria:**
- [ ] Same as US-010 but documentType=03, serie B-prefixed
- [ ] Customer can use DNI (type 1) or no document (type 0)
- [ ] If amount > S/ 700, customer identification is recommended

**Endpoint**: `POST /v1/documents`

### US-012: Emit Credit Note (Nota de Crédito)
**As an** admin or emisor,
**I want to** emit a credit note against an existing invoice,
**So that** I can reverse or adjust a previous sale.

**Acceptance Criteria:**
- [ ] Provide: serie, referenceDocumentId (must exist and be accepted), creditNoteReason (catálogo 09), items
- [ ] Reference document must exist and be in "accepted" status
- [ ] Items can be pre-populated from reference document
- [ ] UBL uses CreditNote namespace (not Invoice)
- [ ] Returns new document with type 07

**Endpoint**: `POST /v1/documents/credit-note`
**Frontend**: `/documents/credit-note`

### US-013: Emit Debit Note (Nota de Débito)
**As an** admin or emisor,
**I want to** emit a debit note to add charges to an invoice.

**Acceptance Criteria:**
- [ ] Same structure as credit note but type 08 and debitNoteReason (catálogo 10)
- [ ] UBL uses DebitNote namespace

**Endpoint**: `POST /v1/documents/debit-note`

### US-014: View Document Detail
**As any** authenticated user,
**I want to** see the full details of a document,
**So that** I can review it and download files.

**Acceptance Criteria:**
- [ ] Returns: all document fields + items + SUNAT response
- [ ] Shows status with appropriate icon/color
- [ ] PDF download button generates A4 PDF on-the-fly
- [ ] XML download button returns the stored XML

**Endpoint**: `GET /v1/documents/{id}`
**Frontend**: `/documents/{id}`

### US-015: List Documents with Filters
**As any** authenticated user,
**I want to** see all my documents with filters,
**So that** I can find specific invoices.

**Acceptance Criteria:**
- [ ] Paginated: page, pageSize (max 100)
- [ ] Filters: documentType, status, dateFrom, dateTo
- [ ] Returns: data array + pagination metadata (totalCount, totalPages)
- [ ] Table shows: fullNumber, type, date, customer, total, status
- [ ] Click on row navigates to detail

**Endpoint**: `GET /v1/documents`
**Frontend**: `/documents`

### US-016: Download PDF
**As any** user,
**I want to** download a PDF of my document.

**Acceptance Criteria:**
- [ ] Returns application/pdf binary
- [ ] PDF shows: company header, document number, customer, items table, totals, hash, QR data area

**Endpoint**: `GET /v1/documents/{id}/pdf`

### US-017: Download XML
**As any** user,
**I want to** download the UBL XML.

**Acceptance Criteria:**
- [ ] Returns application/xml from MinIO storage
- [ ] XML is valid UBL 2.1 with SUNAT namespaces

**Endpoint**: `GET /v1/documents/{id}/xml`

---

## 5. Document Voiding

### US-020: Void a Document (Comunicación de Baja)
**As an** admin,
**I want to** void an accepted document,
**So that** it's cancelled with SUNAT.

**Acceptance Criteria:**
- [ ] Only documents with status "accepted" can be voided
- [ ] Requires voidReason text
- [ ] Creates VoidedDocument with ticket RA-YYYYMMDD-NNN
- [ ] Original document status changes to "voided"
- [ ] HTTP 400 if document is not in accepted status

**Endpoint**: `POST /v1/voided-documents`
**Frontend**: Button on document detail page (admin only)

---

## 6. Series Management

### US-030: Create Series
**As an** admin,
**I want to** create document series (e.g., F001, B001),
**So that** my team can emit documents.

**Acceptance Criteria:**
- [ ] Serie must be exactly 4 characters
- [ ] Unique per tenant + documentType
- [ ] Factura series start with F, Boleta with B
- [ ] Returns SeriesResponse with currentCorrelative

**Endpoint**: `POST /v1/series`
**Frontend**: `/series`

### US-031: List Series
**Endpoint**: `GET /v1/series`

---

## 7. User Management

### US-040: Create User
**As an** admin,
**I want to** add team members with specific roles.

**Acceptance Criteria:**
- [ ] Provide: email, password, fullName, role (admin/emisor/consulta)
- [ ] Email must be unique within tenant
- [ ] Invalid role returns HTTP 400
- [ ] Only admin role can access this endpoint

**Endpoint**: `POST /v1/users`
**Frontend**: `/users`

### US-041: List Users
**Endpoint**: `GET /v1/users`

### US-042: Update User
**Endpoint**: `PUT /v1/users/{id}`

### US-043: Deactivate User
**Endpoint**: `DELETE /v1/users/{id}` (soft delete — sets isActive=false)

---

## 8. API Keys

### US-050: Generate API Key
**As an** admin,
**I want to** create API keys for system integrations.

**Acceptance Criteria:**
- [ ] Provide: name, permissions (emit, query, void)
- [ ] Returns plainTextKey (tk_...) — shown ONLY once
- [ ] Key is hashed (SHA256) before storage
- [ ] Key prefix (first 11 chars) is stored for identification

**Endpoint**: `POST /v1/api-keys`
**Frontend**: `/api-keys`

### US-051: Revoke API Key
**Endpoint**: `DELETE /v1/api-keys/{id}`

---

## 9. Company Settings

### US-060: View/Edit Company Info
**As an** admin,
**I want to** see and update my company data.

**Acceptance Criteria:**
- [ ] GET returns: ruc, razonSocial, nombreComercial, direccion, departamento, provincia, distrito, plan, hasCertificate, environment
- [ ] PUT allows updating: nombreComercial, direccion, departamento, provincia, distrito, primaryColor

**Endpoints**: `GET /v1/tenant`, `PUT /v1/tenant`
**Frontend**: `/settings`

### US-061: Upload Digital Certificate
**As an** admin,
**I want to** upload my digital certificate,
**So that** my documents are digitally signed.

**Acceptance Criteria:**
- [ ] Accepts .pfx, .p12, and .pem formats
- [ ] Max file size: 5MB
- [ ] PEM: password optional. PFX: password required
- [ ] Certificate is validated (parseable, has private key)
- [ ] Returns: subject, issuer, validFrom, expiresAt
- [ ] After upload, new documents are signed automatically

**Endpoint**: `POST /v1/tenant/certificate` (multipart/form-data)
**Frontend**: `/settings` — certificate upload section

### US-062: Switch SUNAT Environment
**Endpoint**: `PUT /v1/tenant/environment` — body: { environment: "beta" | "production" }

---

## 10. Dashboard & Reports

### US-070: View Dashboard
**As any** authenticated user,
**I want to** see a summary of my invoicing activity.

**Acceptance Criteria:**
- [ ] Shows: today/thisMonth/thisYear summaries (totalDocuments, totalAmount, totalIgv, accepted, rejected)
- [ ] Chart: monthly sales bar chart
- [ ] Chart: documents by status pie chart
- [ ] Breakdown by document type with counts and totals

**Endpoint**: `GET /v1/dashboard`
**Frontend**: `/dashboard`

### US-071: Reports with Date Filters
**Frontend**: `/reports` — date range filter, KPI cards, chart, document table

---

## 11. Webhooks

### US-080: CRUD Webhooks
**As an** admin,
**I want to** configure webhooks for real-time notifications.

**Acceptance Criteria:**
- [ ] Create with: url, events array, maxRetries
- [ ] Events: document.created, document.accepted, document.rejected, document.voided
- [ ] Returns HMAC secret on creation (shown once)
- [ ] Deliveries tracked with status (pending/delivered/failed)
- [ ] Exponential backoff retries

**Endpoints**: `GET/POST/PUT/DELETE /v1/webhooks`, `GET /v1/webhooks/{id}/deliveries`
**Frontend**: `/webhooks`

---

## 12. Audit Log

### US-090: View Audit Log
**As an** admin,
**I want to** see all actions performed in my account.

**Acceptance Criteria:**
- [ ] Paginated list with: action, entityType, ipAddress, createdAt
- [ ] Filter by entityType
- [ ] Automatically captures: POST/PUT/DELETE operations

**Endpoint**: `GET /v1/audit-log`
**Frontend**: `/audit-log`

---

## 13. AI Agents

### US-100: AI Document Validator
**As an** emisor,
**I want to** validate a document before emitting,
**So that** I avoid SUNAT rejections.

**Acceptance Criteria:**
- [ ] Validates: document type, serie format, RUC/DNI format, items, currency
- [ ] Returns: is_valid (bool), errors[], warnings[], suggestions[]

**Endpoint**: `POST /v1/ai/validate` (AI service port 8090)

### US-101: AI Item Classifier
**Endpoint**: `POST /v1/ai/classify` — Returns: igv_type, unit_measure, sunat_code, confidence

### US-102: AI Document Extractor
**Endpoint**: `POST /v1/ai/extract` — Extracts structured data from raw text/OCR

### US-103: AI Copilot Chat
**As any** user,
**I want to** chat with an AI assistant about invoicing rules.

**Acceptance Criteria:**
- [ ] Responds to questions about facturas, boletas, IGV, series, voiding, etc.
- [ ] Returns: response text, sources, confidence, suggestions
- [ ] Clickable suggestions for follow-up questions

**Endpoint**: `POST /v1/ai/chat`
**Frontend**: `/ai`

### US-104: AI Analyst
**Endpoint**: `POST /v1/ai/analyze` — Returns insights, recommendations, alerts from dashboard data

### US-105: AI Conciliator
**Endpoint**: `POST /v1/ai/reconcile` — Matches documents with bank payments

---

## 14. Health & Monitoring

### US-110: Health Checks
**Acceptance Criteria:**
- [ ] `/health` returns overall status + individual checks (postgresql, nats, minio)
- [ ] `/health/ready` returns readiness status
- [ ] `/health/live` returns liveness (process is running)
- [ ] `/api/ping` returns service name, version, environment, timestamp

### US-111: Prometheus Metrics
**Endpoint**: `GET /metrics` — Returns: tukifact_tenants_total, tukifact_documents_total, tukifact_users_total

---

## 15. Plans & Pricing

### US-120: View Plans
**Acceptance Criteria:**
- [ ] Public endpoint (no auth required)
- [ ] Returns all active plans with: name, priceMonthly, maxDocumentsPerMonth, features

**Endpoint**: `GET /v1/plans`
**Frontend**: `/plan`

---

## 16. Rate Limiting

**Acceptance Criteria:**
- [ ] Per-tenant, per-endpoint sliding window (1 minute)
- [ ] Limits based on plan (Free=10/min, Emprendedor=100, Negocio=300, etc.)
- [ ] Response headers: X-RateLimit-Limit, X-RateLimit-Remaining
- [ ] HTTP 429 when exceeded with Retry-After: 60 header

---

## 17. Security

**Acceptance Criteria:**
- [ ] JWT authentication required for all endpoints except: /health/*, /api/ping, /v1/auth/*, /v1/plans
- [ ] Row Level Security in PostgreSQL — each tenant only sees their own data
- [ ] CORS configured for frontend origin only
- [ ] Security headers: X-Frame-Options, X-Content-Type-Options, HSTS, CSP, Referrer-Policy
- [ ] Passwords hashed with BCrypt (work factor 12)
- [ ] API keys hashed with SHA256
- [ ] Certificate passwords stored (encrypted in production)

---

## 18. Edge Cases & Error States

- Empty items array → HTTP 400
- Non-existent series → HTTP 400 "Serie no encontrada"
- Duplicate RUC registration → HTTP 409
- Expired JWT → HTTP 401 (auto-refresh attempt, then redirect to login)
- Document not found → HTTP 404
- Void non-accepted document → HTTP 400 "Solo se pueden anular documentos aceptados"
- Invalid role in user creation → HTTP 400
- File upload > 5MB → HTTP 413
- Rate limit exceeded → HTTP 429
- Invalid certificate format → HTTP 400 with specific error message
