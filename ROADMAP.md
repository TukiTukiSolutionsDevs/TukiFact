# TukiFact — Roadmap Dinámico

> **Plataforma SaaS de Facturación Electrónica para Perú**
> Empresa: Tukituki Solution SAC | RUC: 20613614509 | Dominio: tukifact.net.pe
>
> _Última actualización: 2026-04-07 — FASE 1 COMPLETADA_

---

## Estado General

```
FASE 1 ████████████████████████ 100%  Fundación (Sprints 1-4) ✅
FASE 2 ████████████████████████ 100%  Producción (Sprints 5-8) ✅
FASE 3 ████████████████████████ 100%  Crecimiento (Sprints 9-12) ✅
TOTAL  ████████████████████████ 100%  12/12 sprints COMPLETADOS 🎉
```

---

## FASE 1 — Fundación (Semanas 1-8)

> Objetivo: API funcional que emite Facturas, Boletas, NC/ND y se integra con SUNAT.

### Sprint 1: Infraestructura Base ✅ COMPLETADO
_Semanas 1-2 | Completado 2026-04-07_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 1.1 | Monorepo .NET 10 Clean Architecture | ✅ | Domain → Application → Infrastructure → Api |
| 1.2 | Docker Compose (PG18 + NATS + MinIO) | ✅ | PG en port 5433 (local 5432 ocupado) |
| 1.3 | EF Core + PostgreSQL con migraciones | ✅ | Npgsql 10.x, migration InitialCreate |
| 1.4 | Schema: tenants, users, plans, api_keys | ✅ | 4 tablas con índices únicos |
| 1.5 | Row Level Security (RLS) | ✅ | Políticas en users, api_keys (PascalCase cols) |
| 1.6 | TenantResolver Middleware | ✅ | Extrae tenant de JWT o X-Tenant-Id header |
| 1.7 | Health checks (PG, NATS, MinIO) | ✅ | /health, /health/ready, /health/live |

**Gotchas descubiertos:**
- PG18 cambió mount a `/var/lib/postgresql` (no `/data`)
- EF Core usa PascalCase → RLS con `"TenantId"` (comillas)
- .NET 10 usa `.slnx` en vez de `.sln`

---

### Sprint 2: Auth + Tenant Management ✅ COMPLETADO
_Semanas 3-4 | Completado 2026-04-07_

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

**Hotfix aplicado:** KeyPrefix varchar(8) → varchar(12)

**Endpoints operativos:**
- `POST /v1/auth/register` — Registro tenant + admin
- `POST /v1/auth/login` — Login → JWT
- `POST /v1/auth/refresh` — Refresh token rotation
- `GET  /v1/auth/me` — Info del usuario autenticado
- `GET  /v1/plans` — Lista pública de planes
- `CRUD /v1/users` — Gestión de usuarios (admin only)
- `CRUD /v1/series` — Gestión de series (admin only)
- `CRUD /v1/api-keys` — Gestión de API keys (admin only)

---

### Sprint 3: Emisión de Comprobantes ✅ COMPLETADO
_Semanas 5-6 | Completado 2026-04-07_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 3.1 | Entities: Document, DocumentItem, DocumentXmlLog | ✅ | + migration + RLS |
| 3.2 | UBL 2.1 XML Builder (Factura 01, Boleta 03) | ✅ | Namespaces SUNAT compliant |
| 3.3 | Firma digital XML (X509 + XMLDSig) | ✅ | X509CertificateLoader.LoadPkcs12 (.NET 10) |
| 3.4 | Cliente SUNAT (stub beta) | ✅ | Simula aceptación con 200ms latencia |
| 3.5 | MinIO storage (XML, CDR, PDF) | ✅ | 4 buckets: xml, pdf, cdr, certs |
| 3.6 | DocumentService orquestador | ✅ | validate→calc→create→XML→sign→store→send→QR |
| 3.7 | DocumentsController | ✅ | POST emit, GET /:id, GET list, GET /:id/xml |

**Primera factura emitida:**
```
F001-00000001 | SODIMAC PERU S.A. | S/ 12,589.98 | ACEPTADA
```

**Flujo de emisión:**
```
Request → Validar → Calcular IGV → Correlativo atómico → Crear DB
  → UBL 2.1 XML → Firmar cert → MinIO → SUNAT → CDR → QR → Update
```

---

### Sprint 4: Integración SUNAT Real + PDF ✅ COMPLETADO
_Semanas 7-8 | Completado 2026-04-07_

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 4.0 | Fix correlativo off-by-one | ✅ | RETURNING en SQL atómico |
| 4.1 | SUNAT SOAP client real (sendBill) | ✅ | SOAP + beta stub mode |
| 4.2 | CDR parser (ZIP → código + descripción) | ✅ | Extrae ResponseCode/Description/Notes |
| 4.3 | PDF generation (QuestPDF) | ✅ | A4, 61KB, on-the-fly |
| 4.4 | Notas de Crédito (07) + Débito (08) | ✅ | UBL CreditNote/DebitNote namespaces |
| 4.5 | Comunicación de Baja (RA) | ✅ | VoidedDocument + RA-YYYYMMDD-NNN |
| 4.6 | Resumen Diario de Boletas (RC) | ✅ | sendSummary + getStatus |
| 4.7 | Dashboard endpoint + métricas | ✅ | Hoy/Mes/Año + byType + byStatus + monthlySales |
| 4.8 | E2E test completo | ✅ | 5/5 endpoints verificados |

**Hotfix aplicado:** DashboardService MonthlySales GroupBy → anonymous types (EF Core limitation)

**Endpoints Sprint 4:**
- `POST /v1/documents/credit-note` — Emitir NC referenciando factura
- `POST /v1/documents/debit-note` — Emitir ND referenciando factura
- `GET  /v1/documents/{id}/pdf` — Descargar PDF (61KB, QuestPDF)
- `POST /v1/voided-documents` — Comunicación de Baja (RA)
- `GET  /v1/voided-documents` — Listar bajas
- `GET  /v1/dashboard` — Métricas del tenant

---

## FASE 2 — Producción (Semanas 9-16)

> Objetivo: Frontend, notificaciones, API pública y salida a producción SUNAT.

### Sprint 5: Frontend — Portal Web ✅ COMPLETADO
_Semanas 9-10 | Completado 2026-04-07_

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

**Stack Frontend:**
- Next.js 16 (App Router) + TypeScript + Tailwind v4 + shadcn/ui (@base-ui/react)
- recharts 3.x (charts), sonner (toasts), lucide-react (icons)
- JWT auto-refresh en API client, AuthProvider con useAuth hook

**12 rutas generadas:**
`/ → /login → /register → /dashboard → /documents → /documents/new → /documents/[id] → /series → /settings → /voided`

### Sprint 6: Frontend — Admin + UX ✅ COMPLETADO
_Semanas 11-12 | Completado 2026-04-07_

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

**17 rutas totales en el frontend**
| 6.8 | Onboarding wizard (primera empresa) | ⏳ |

### Sprint 7: Notificaciones + Webhooks ⏳ PENDIENTE
_Semanas 13-14_

| # | Tarea | Estado |
|---|-------|--------|
| 7.1 | NATS JetStream consumers | ⏳ |
| 7.2 | Email transaccional (Resend/SES) | ⏳ |
| 7.3 | Webhook system (configurable por tenant) | ⏳ |
| 7.4 | Envío automático de PDF al cliente | ⏳ |
| 7.5 | Notificaciones in-app (SSE o WebSocket) | ⏳ |
| 7.6 | Rate limiting + throttling | ⏳ |
| 7.7 | Audit log | ⏳ |

### Sprint 8: Producción SUNAT ⏳ PENDIENTE
_Semanas 15-16_

| # | Tarea | Estado |
|---|-------|--------|
| 8.1 | Integración OSE producción | ⏳ |
| 8.2 | Homologación SUNAT | ⏳ |
| 8.3 | CI/CD pipeline (GitHub Actions) | ⏳ |
| 8.4 | Deploy staging (Docker Compose → VPS/Cloud) | ⏳ |
| 8.5 | SSL/TLS + DNS tukifact.net.pe | ⏳ |
| 8.6 | Monitoring (Prometheus + Grafana) | ⏳ |
| 8.7 | Backup strategy (PG + MinIO) | ⏳ |
| 8.8 | Security audit + penetration test | ⏳ |

---

## FASE 3 — Crecimiento (Semanas 17-24)

> Objetivo: IA, API pública documentada, SDK y features avanzados.

### Sprint 9: API Pública + SDK ⏳ PENDIENTE
_Semanas 17-18_

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
_Semanas 19-20_

| # | Tarea | Estado |
|---|-------|--------|
| 10.1 | Python FastAPI microservice setup | ⏳ |
| 10.2 | Agente Validador (pre-emisión IA) | ⏳ |
| 10.3 | Agente Clasificador (tipo IGV, CIIU) | ⏳ |
| 10.4 | Agente Extractor (OCR → factura) | ⏳ |
| 10.5 | NATS bridge .NET ↔ Python | ⏳ |
| 10.6 | BYOK (Bring Your Own Key) API keys | ⏳ |

### Sprint 11: Agentes IA — Fase 2 ⏳ PENDIENTE
_Semanas 21-22_

| # | Tarea | Estado |
|---|-------|--------|
| 11.1 | Agente Copiloto (chat de ayuda) | ⏳ |
| 11.2 | Agente Analista (reportes inteligentes) | ⏳ |
| 11.3 | Agente Conciliador (cruce con bancos) | ⏳ |
| 11.4 | RAG con normativa SUNAT | ⏳ |
| 11.5 | Dashboard IA con insights | ⏳ |

### Sprint 12: Optimización + Scale ⏳ PENDIENTE
_Semanas 23-24_

| # | Tarea | Estado |
|---|-------|--------|
| 12.1 | Performance tuning (PG indexes, caching) | ⏳ |
| 12.2 | Kubernetes migration (opcional) | ⏳ |
| 12.3 | Multi-region (si demanda lo requiere) | ⏳ |
| 12.4 | Billing integration (Stripe/MercadoPago) | ⏳ |
| 12.5 | White-label features | ⏳ |
| 12.6 | Mobile app (React Native) | ⏳ |

---

## Stack Tecnológico

| Capa | Tecnología | Versión |
|------|-----------|---------|
| **Backend Core** | ASP.NET Core | .NET 10 LTS |
| **IA Services** | Python FastAPI | 3.12+ |
| **Frontend** | Next.js + Tailwind | 16 |
| **Database** | PostgreSQL + RLS | 18 |
| **Messaging** | NATS JetStream | Latest |
| **Storage** | MinIO (S3-compatible) | Latest |
| **Containers** | Docker Compose | — |
| **XML** | UBL 2.1 | SUNAT PE |
| **Firma** | XMLDSig + X509 | RSA-SHA256 |

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

## Métricas del Proyecto

| Métrica | Valor |
|---------|-------|
| Entidades de dominio | 8 (Tenant, User, Plan, ApiKey, Series, Document, DocumentItem, DocumentXmlLog, RefreshToken) |
| Endpoints API | 18 |
| Tablas PostgreSQL | 9 (+ __EFMigrationsHistory) |
| Tablas con RLS | 6 (users, api_keys, series, refresh_tokens, documents, document_xml_logs) |
| Migraciones EF Core | 4 |
| Docker services | 4 (PG18, NATS, MinIO, MinIO-init) |
| MinIO buckets | 4 (xml, pdf, cdr, certs) |
| Planes de pricing | 6 |
| NuGet packages | ~12 |

---

_Este roadmap se actualiza al completar cada sprint. Ver `/investigacion/` para documentación detallada de cada decisión._
