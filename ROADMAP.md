# TukiFact — Roadmap Dinámico

> **Plataforma SaaS de Facturación Electrónica para Perú**
> Empresa: Tukituki Solution SAC | RUC: 20613614509 | Dominio: tukifact.net.pe
>
> _Última actualización: 2026-04-14_

---

## Estado General

```
FASE 1  ████████████████████████ 100%  Fundación — API + DB + Auth (Sprints 1-4) ✅
FASE 2  █████████████████░░░░░░  75%   Producción — Frontend + Infra (Sprints 5-8)
FASE 3  ░░░░░░░░░░░░░░░░░░░░░░░  0%   Crecimiento — IA + SDK + Scale (Sprints 9-12)
EXTRA   ██████░░░░░░░░░░░░░░░░░  25%   Backoffice + DevOps (Sprint B1-B3)
MEJORA  ████████████████████████ 100%  Batch A+B+C Backend + Frontend + Migrado ✅
TOTAL   █████████████████░░░░░░  72%   Backend COMPLETO, Deploy pendiente
```

## Credenciales Actuales

| Rol | Email | Password |
|-----|-------|----------|
| Tenant Admin | admin@tukifact.net.pe | TukiFact2026! |
| SuperAdmin (Backoffice) | superadmin@tukifact.net.pe | SuperAdmin2026! |

## Docker Prod Stack (7 servicios HEALTHY)

```
postgres ✅ | nats ✅ | minio ✅ | api ✅ | web (port 3001) ✅ | ai ✅ | nginx (port 80) ✅
```

---

## FASE 1 — Fundación (Sprints 1-4) ✅ COMPLETADA

> Objetivo: API funcional que emite Facturas, Boletas, NC/ND y se integra con SUNAT.

### Sprint 1: Infraestructura Base ✅ COMPLETADO
_Completado 2026-04-07_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 1.1 | Monorepo .NET 10 Clean Architecture | ✅ | Domain → Application → Infrastructure → Api |
| 1.2 | Docker Compose (PG18 + NATS + MinIO) | ✅ | PG en port 5433 (local 5432 ocupado) |
| 1.3 | EF Core + PostgreSQL con migraciones | ✅ | Npgsql 10.x, migration InitialCreate |
| 1.4 | Schema: tenants, users, plans, api_keys | ✅ | 4 tablas con índices únicos |
| 1.5 | Row Level Security (RLS) | ✅ | Políticas en users, api_keys (PascalCase cols) |
| 1.6 | TenantResolver Middleware | ✅ | Extrae tenant de JWT o X-Tenant-Id header |
| 1.7 | Health checks (PG, NATS, MinIO) | ✅ | /health, /health/ready, /health/live |

### Sprint 2: Auth + Tenant Management ✅ COMPLETADO
_Completado 2026-04-07_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 2.1 | Registro empresa (tenant) con RUC | ✅ | Crea tenant + admin user + plan Free |
| 2.2 | Login JWT (access + refresh token) | ✅ | HS256, 60min access, 7d refresh |
| 2.3 | Middleware JWT + TenantResolver update | ✅ | Pipeline: Auth → TenantResolver → Authorization |
| 2.4 | CRUD API Keys | ✅ | Generación tk_*, SHA256 hash, revocación |
| 2.5 | RBAC (admin, emisor, consulta) | ✅ | Roles en JWT claims + [Authorize(Roles)] |
| 2.6 | Entity Series + CRUD | ✅ | Unique(TenantId, DocumentType, Serie) |
| 2.7 | Seed planes pricing | ✅ | 6 planes: Free → Empresa ($0-$299) |
| 2.8 | CRUD Users | ✅ | Create, Update, soft-delete |

### Sprint 3: Emisión de Comprobantes ✅ COMPLETADO
_Completado 2026-04-07_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 3.1 | Entities: Document, DocumentItem, DocumentXmlLog | ✅ | + migration + RLS |
| 3.2 | UBL 2.1 XML Builder (Factura 01, Boleta 03) | ✅ | Namespaces SUNAT compliant |
| 3.3 | Firma digital XML (X509 + XMLDSig) | ✅ | X509CertificateLoader.LoadPkcs12 (.NET 10) |
| 3.4 | Cliente SUNAT (stub beta) | ✅ | Simula aceptación con 200ms latencia |
| 3.5 | MinIO storage (XML, CDR, PDF) | ✅ | 4 buckets: xml, pdf, cdr, certs |
| 3.6 | DocumentService orquestador | ✅ | validate→calc→create→XML→sign→store→send→QR |
| 3.7 | DocumentsController | ✅ | POST emit, GET /:id, GET list, GET /:id/xml |

### Sprint 4: Integración SUNAT Real + PDF ✅ COMPLETADO
_Completado 2026-04-07_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 4.1 | SUNAT SOAP client real (sendBill) | ✅ | SOAP + beta stub mode |
| 4.2 | CDR parser (ZIP → código + descripción) | ✅ | Extrae ResponseCode/Description/Notes |
| 4.3 | PDF generation (QuestPDF) | ✅ | A4, 61KB, on-the-fly |
| 4.4 | Notas de Crédito (07) + Débito (08) | ✅ | UBL CreditNote/DebitNote namespaces |
| 4.5 | Comunicación de Baja (RA) | ✅ | VoidedDocument + RA-YYYYMMDD-NNN |
| 4.6 | Resumen Diario de Boletas (RC) | ✅ | sendSummary + getStatus |
| 4.7 | Dashboard endpoint + métricas | ✅ | Hoy/Mes/Año + byType + byStatus + monthlySales |
| 4.8 | E2E test completo | ✅ | 5/5 endpoints verificados |

---

## FASE 2 — Producción (Sprints 5-8) — 75% EN PROGRESO

> Objetivo: Frontend, notificaciones, API pública y salida a producción SUNAT.

### Sprint 5: Frontend — Portal Web ✅ COMPLETADO
_Completado 2026-04-07_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 5.1 | Next.js 16 + Tailwind v4 + shadcn/ui | ✅ | 15 componentes UI instalados |
| 5.2 | Auth pages (login, register) | ✅ | JWT + auto-refresh + AuthProvider |
| 5.3 | Dashboard con métricas | ✅ | KPI cards + BarChart + PieChart (recharts) |
| 5.4 | Emisión de comprobantes | ✅ | Formulario dinámico con cálculo IGV en vivo |
| 5.5 | Lista de comprobantes + filtros | ✅ | Tabla paginada con filtros tipo/estado |
| 5.6 | Visor de documento | ✅ | Detalle completo + PDF/XML download |
| 5.7 | Gestión de series | ✅ | Lista + dialog crear serie |
| 5.8 | Configuración + Bajas | ✅ | Settings + voided documents table |

**17 rutas frontend operativas**

### Sprint 6: Frontend — Admin + UX ✅ COMPLETADO
_Completado 2026-04-07_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 6.1 | Gestión de usuarios + roles | ✅ | CRUD completo + toggle activo + dialogs |
| 6.2 | Gestión de API Keys | ✅ | Generar + reveal plainTextKey + copiar + revocar |
| 6.3 | Plan actual + pricing cards | ✅ | Grid de planes, actual destacado |
| 6.4 | Reportes (ventas/impuestos) | ✅ | Filtro fecha + 6 KPIs + chart + tabla |
| 6.5 | Notas de Crédito desde UI | ✅ | 3 pasos: buscar ref → motivo → items editables |
| 6.6 | Anulación desde detalle | ✅ | Botón Anular con dialog confirmación |
| 6.7 | Dark mode system | ✅ | ThemeToggle Sun/Moon + next-themes |
| 6.8 | Onboarding wizard | ✅ | 4 pasos con progress bar + checks async |

### Sprint 7: Notificaciones + Webhooks ⏳ PENDIENTE
_Prioridad: MEDIA — después de Backoffice Frontend y Deploy_

| # | Tarea | Estado | Dependencia |
|---|-------|--------|-------------|
| 7.1 | NATS JetStream consumers | ⏳ | — |
| 7.2 | Email transaccional (Resend/SES) | ⏳ | Dominio DNS |
| 7.3 | Webhook system (configurable por tenant) | ⏳ | — |
| 7.4 | Envío automático de PDF al cliente | ⏳ | 7.2 |
| 7.5 | Notificaciones in-app (SSE o WebSocket) | ⏳ | — |
| 7.6 | Rate limiting + throttling | ⏳ | — |
| 7.7 | Audit log (backend existe, falta UI) | ⏳ | — |

### Sprint 8: Producción SUNAT + Deploy ⏳ PENDIENTE
_Prioridad: ALTA — BLOQUEANTE para ir a producción_

| # | Tarea | Estado | Dependencia |
|---|-------|--------|-------------|
| 8.1 | Integración OSE/SUNAT producción real | ⏳ | Certificado digital real |
| 8.2 | Homologación SUNAT (set de pruebas) | ⏳ | 8.1 |
| 8.3 | CI/CD pipeline (GitHub Actions) | ⏳ | — |
| 8.4 | Deploy VPS (Docker Compose) | ⏳ | 8.3 |
| 8.5 | SSL/TLS + DNS tukifact.net.pe | ⏳ | VPS |
| 8.6 | nginx.conf server_name dominio real | ⏳ | 8.5 |
| 8.7 | Backup strategy (PG + MinIO) | ⏳ | VPS |
| 8.8 | Monitoring (Prometheus + Grafana) | ⏳ | VPS |

---

## EXTRA — Backoffice + Operaciones (Sprints B1-B3)

> Resultado de la auditoría vs Nubefact/PSE (2026-04-13).
> Sin backoffice NO se puede operar como SaaS — es BLOQUEANTE.

### Sprint B1: Backoffice Backend ✅ COMPLETADO
_Completado 2026-04-13_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| B1.1 | PlatformUser entity + EF config | ✅ | Tabla platform_users (sin migration formal) |
| B1.2 | BackofficeAuthController (login separado) | ✅ | JWT sin tenant_id, con platform_user claim |
| B1.3 | Dashboard global (cross-tenant) | ✅ | SET LOCAL row_security = off |
| B1.4 | Tenants CRUD (list, detail, suspend, activate, change plan) | ✅ | 5 endpoints |
| B1.5 | Document search cross-tenant (soporte) | ✅ | Buscar por RUC, serie, correlativo |
| B1.6 | Platform employees list | ✅ | superadmin only |
| B1.7 | SuperAdmin seeder automático | ✅ | superadmin@tukifact.net.pe |
| B1.8 | TenantResolverMiddleware bypass /v1/backoffice | ✅ | — |
| B1.9 | Docker stack 10 fixes + 7 services healthy | ✅ | kerberos, DataProtection, CSP, etc. |

**9 endpoints backoffice operativos:**
- `POST /v1/backoffice/auth/login` — Login superadmin/support/ops
- `GET  /v1/backoffice/dashboard` — Métricas globales plataforma
- `GET  /v1/backoffice/tenants` — Lista tenants paginada + search
- `GET  /v1/backoffice/tenants/{id}` — Detalle tenant + users + stats
- `PUT  /v1/backoffice/tenants/{id}/suspend` — Suspender tenant
- `PUT  /v1/backoffice/tenants/{id}/activate` — Activar tenant
- `PUT  /v1/backoffice/tenants/{id}/plan` — Cambiar plan
- `GET  /v1/backoffice/documents` — Buscar documentos cross-tenant
- `GET  /v1/backoffice/employees` — Listar empleados plataforma

### Sprint B2: Backoffice Frontend ⏳ PRÓXIMO
_Prioridad: ALTA — darle cara al backend que ya existe_

| # | Tarea | Estado | Endpoint backend |
|---|-------|--------|------------------|
| B2.1 | Layout backoffice (sidebar, header, auth guard) | ⏳ | — |
| B2.2 | Login backoffice (/backoffice/login) | ⏳ | POST /v1/backoffice/auth/login |
| B2.3 | Dashboard global (/backoffice/dashboard) | ⏳ | GET /v1/backoffice/dashboard |
| B2.4 | Lista de tenants (/backoffice/tenants) | ⏳ | GET /v1/backoffice/tenants |
| B2.5 | Detalle tenant (/backoffice/tenants/[id]) | ⏳ | GET /v1/backoffice/tenants/{id} |
| B2.6 | Acciones tenant (suspender, activar, cambiar plan) | ⏳ | PUT suspend/activate/plan |
| B2.7 | Búsqueda documentos soporte (/backoffice/documents) | ⏳ | GET /v1/backoffice/documents |
| B2.8 | Empleados plataforma (/backoffice/employees) | ⏳ | GET /v1/backoffice/employees |

### Sprint B3: Backoffice Avanzado ⏳ FUTURO
_Prioridad: MEDIA — después de producción_

| # | Tarea | Estado |
|---|-------|--------|
| B3.1 | CRUD empleados plataforma (crear/editar/desactivar) | ⏳ |
| B3.2 | Logs de actividad backoffice | ⏳ |
| B3.3 | Gestión de suscripciones + cobros | ⏳ |
| B3.4 | Reportes plataforma (MRR, churn, growth) | ⏳ |
| B3.5 | Soporte: impersonar tenant (ver como ellos ven) | ⏳ |
| B3.6 | Configuración global plataforma | ⏳ |

---

## FASE 3 — Crecimiento (Sprints 9-12)

> Objetivo: IA, API pública documentada, SDK y features avanzados.
> _DESPUÉS de estar en producción con clientes reales._

### Sprint 9: API Pública + SDK ⏳ PENDIENTE

| # | Tarea | Estado |
|---|-------|--------|
| 9.1 | API versionada con OpenAPI 3.1 | ⏳ |
| 9.2 | API Key authentication para terceros | ⏳ |
| 9.3 | Rate limiting por plan | ⏳ |
| 9.4 | SDK Node.js/TypeScript | ⏳ |
| 9.5 | SDK Python | ⏳ |
| 9.6 | Documentación interactiva (Redoc/Scalar) | ⏳ |
| 9.7 | Sandbox/playground para developers | ⏳ |

### Sprint 10: Agentes IA — Fase 1 ⏳ PENDIENTE

| # | Tarea | Estado |
|---|-------|--------|
| 10.1 | Python FastAPI microservice setup | ⏳ |
| 10.2 | Agente Validador (pre-emisión IA) | ⏳ |
| 10.3 | Agente Clasificador (tipo IGV, CIIU) | ⏳ |
| 10.4 | Agente Extractor (OCR → factura) | ⏳ |
| 10.5 | NATS bridge .NET ↔ Python | ⏳ |
| 10.6 | BYOK (Bring Your Own Key) API keys | ⏳ |

### Sprint 11: Agentes IA — Fase 2 ⏳ PENDIENTE

| # | Tarea | Estado |
|---|-------|--------|
| 11.1 | Agente Copiloto (chat de ayuda) | ⏳ |
| 11.2 | Agente Analista (reportes inteligentes) | ⏳ |
| 11.3 | Agente Conciliador (cruce con bancos) | ⏳ |
| 11.4 | RAG con normativa SUNAT | ⏳ |
| 11.5 | Dashboard IA con insights | ⏳ |

### Sprint 12: Optimización + Scale ⏳ PENDIENTE

| # | Tarea | Estado |
|---|-------|--------|
| 12.1 | Performance tuning (PG indexes, caching) | ⏳ |
| 12.2 | Kubernetes migration (opcional) | ⏳ |
| 12.3 | Multi-region (si demanda lo requiere) | ⏳ |
| 12.4 | Billing integration (Stripe/MercadoPago) | ⏳ |
| 12.5 | White-label features | ⏳ |
| 12.6 | Mobile app (React Native) | ⏳ |

---

## Orden de Ejecución Recomendado

```
AHORA        → Batch D: Docker rebuild web + Deploy VPS + SSL + dominio
DESPUÉS      → Sprint B2: Backoffice Frontend (darle cara al backend)
DESPUÉS      → Sprint 8.1-8.2: SUNAT Producción + Homologación
DESPUÉS      → Sprint 7: Notificaciones + Email + Webhooks
DESPUÉS      → Sprint B3: Backoffice Avanzado
FUTURO       → Fase 3: API Pública + IA + Scale
```

### Funcionalidades Pendientes de Auditoría (vs Nubefact/PSE)

| Categoría | Feature | Estado | Batch/Sprint |
|-----------|---------|--------|-------------|
| Funcional | Certificado Digital UI (subir .pfx) | ✅ | Existente |
| Funcional | Catálogo de productos/servicios | ✅ | Existente |
| Funcional | Directorio de clientes frecuentes | ✅ | Existente |
| Funcional | Email envío automático al cliente | ✅ | Batch A |
| Funcional | Guías de Remisión (T09) | ✅ Backend + Frontend | Batch A |
| Funcional | Retenciones (R20) y Percepciones (R40) | ✅ Backend + Frontend | Batch C |
| Funcional | SIRE Integración (5 endpoints) | ✅ Backend | Batch C |
| SUNAT | Cliente SOAP producción real | ⏳ | Sprint 8 |
| SUNAT | Consulta de validez CPE | ⏳ | Sprint 8 |
| SUNAT | Tipo de cambio automático (SBS) | ✅ Backend | Batch B |
| Diferenciación | Facturación recurrente | ✅ Backend + Frontend | Batch C |
| Diferenciación | Multi-moneda (USD/EUR) | ✅ Backend | Batch C |
| Diferenciación | Cotizaciones → Factura | ✅ Backend + Frontend | Batch C |
| Diferenciación | Detracciones SPOT | ✅ Backend | Batch B |
| Diferenciación | Catálogos SUNAT completos | ✅ Backend | Batch B |

---

## Stack Tecnológico

| Capa | Tecnología | Versión |
|------|-----------|---------|
| **Backend Core** | ASP.NET Core | .NET 10 |
| **IA Services** | Python FastAPI | 3.12+ |
| **Frontend** | Next.js + Tailwind | 16 + v4 |
| **UI Components** | shadcn/ui | @base-ui/react |
| **Database** | PostgreSQL + RLS | 18 |
| **Messaging** | NATS JetStream | Latest |
| **Storage** | MinIO (S3-compatible) | Latest |
| **Containers** | Docker Compose (prod) | 7 servicios |
| **XML** | UBL 2.1 | SUNAT PE |
| **Firma** | XMLDSig + X509 | RSA-SHA256 |
| **Reverse Proxy** | Nginx | Latest |

## Modelo de Pricing

| Plan | Precio/mes | Docs/mes | API | IA | Usuarios |
|------|-----------|----------|-----|----|----------|
| Free | S/ 0 | 50 | No | No | 1 |
| Emprendedor | S/ 39 | 300 | Sí | No | 3 |
| Negocio | S/ 79 | 1,000 | Sí | Básica | 10 |
| Developer | S/ 99 | 1,000 | Sí | Copilot | 5 |
| Profesional | S/ 149 | 3,000 | Sí | Full | 25 |
| Empresa | S/ 299 | 10,000 | Sí | Full+Agentes | Ilimitados |

---

## Mejora Producción — Batches A+B+C ✅ COMPLETADO

> Resultado del análisis competitivo vs Nubefact/PSE. 3 batches para pasar de MVP a producto competitivo.

### Batch A — "Sin esto no vendés" ✅ Backend + Migrado + Frontend
_Completado 2026-04-13 (backend) + 2026-04-14 (frontend)_

| Item | Feature | Archivos |
|------|---------|----------|
| A1 | Guías de Remisión (GRE T09) | Entity + UBL + Controller + Service + Frontend |
| A2 | Email transaccional | EmailLog entity + Service + Templates |
| A3 | Password Reset | PasswordResetToken + Controller |

### Batch B — "Competitivo con Nubefact" ✅ Backend + Migrado
_Completado 2026-04-13_

| Item | Feature | Archivos |
|------|---------|----------|
| B1 | Tipo de Cambio (SBS automático) | ExchangeRate entity + Service + Controller |
| B2 | Detracciones SPOT | Document fields + UBL extension |
| B3 | Catálogos SUNAT | SunatCatalog + SunatCatalogCode + Seed 600+ |
| B4 | Códigos de Detracción | DetractionCode entity + Seed |

### Batch C — "MEJOR que Nubefact" ✅ Backend + Migrado + Frontend
_Completado 2026-04-14_

| Item | Feature | Archivos |
|------|---------|----------|
| C1 | Retenciones Electrónicas (tipo 20) | Entity + UBL 2.0 + Controller + Frontend |
| C2 | Percepciones Electrónicas (tipo 40) | Entity + UBL 2.0 + Controller + Frontend |
| C3 | SIRE Integración | OAuth2 REST Client + 5 endpoints |
| C4 | Multi-moneda | Document ExchangeRate + UBL PaymentExchangeRate |
| C5 | Facturación Recurrente | Entity + BackgroundService + Controller + Frontend |
| C6 | Cotizaciones → Factura | Entity + Controller (convert-to-invoice) + Frontend |

**Totales**: 37+ archivos backend, 14 archivos frontend, 34 tablas DB, 10 migraciones

### Batch D — "Deploy + Infraestructura" 🔲 PENDIENTE

| Item | Feature |
|------|---------|
| D1 | Docker rebuild web (incluir nuevas páginas) |
| D2 | SSL/TLS + dominio tukifact.net.pe |
| D3 | CI/CD pipeline (GitHub Actions) |
| D4 | Monitoring (Prometheus + Grafana) |
| D5 | Backup strategy (PG + MinIO) |

---

## Métricas del Proyecto

| Métrica | Valor |
|---------|-------|
| Entidades de dominio | 22 (Batch A+B+C) |
| Endpoints API (tenant) | 40+ |
| Endpoints API (backoffice) | 9 |
| Tablas PostgreSQL | 34 |
| Tablas con RLS | 6 |
| Migraciones EF Core | 10 |
| Docker services (prod) | 7 |
| MinIO buckets | 4 (xml, pdf, cdr, certs) |
| Planes de pricing | 6 |
| Rutas frontend (tenant) | 31 (17 original + 14 Batch A+B+C) |
| Rutas frontend (backoffice) | 0 → 8 (Sprint B2) |
| Archivos frontend nuevos | 14 páginas Batch A+B+C |

---

_Este roadmap se actualiza al completar cada sprint. Ver `/investigacion/` para documentación detallada._
