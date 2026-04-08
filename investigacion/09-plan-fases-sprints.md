# 09 - Plan de Fases y Sprints

## Consideraciones
- **Equipo**: 2-3 desarrolladores
- **Sprint**: 2 semanas
- **Sin deadline**: Calidad sobre velocidad
- **Stack**: .NET 10 LTS + Python FastAPI + Next.js + PostgreSQL 18 + NATS + MinIO + Docker

---

## FASE 1 — FUNDACIÓN (Sprints 1-4 | ~8 semanas)

### Sprint 1: Infraestructura Base (Semanas 1-2)
- [ ] Crear repositorio Git con estructura de monorepo
- [ ] Docker Compose con: PostgreSQL 18 + NATS + MinIO
- [ ] Proyecto ASP.NET Core .NET 10 con estructura Clean Architecture
- [ ] Configurar EF Core + PostgreSQL con migraciones
- [ ] Implementar schema de DB: tenants, users, plans, api_keys
- [ ] RLS (Row Level Security) en PostgreSQL
- [ ] Middleware TenantResolver que setea `app.current_tenant`
- [ ] Health checks para todos los servicios

### Sprint 2: Auth + Tenant Management (Semanas 3-4)
- [ ] Registro de empresa (tenant) con RUC
- [ ] Registro de usuario administrador
- [ ] Login con JWT (access + refresh token)
- [ ] Middleware de autenticación JWT
- [ ] CRUD de API Keys (generar, listar, revocar)
- [ ] Autenticación por API Key (header X-Api-Key)
- [ ] RBAC básico: admin, emisor, consulta
- [ ] Schema DB: series
- [ ] CRUD de series por tenant

### Sprint 3: Catálogos + Tax Engine Básico (Semanas 5-6)
- [ ] Cargar catálogos SUNAT core (01, 02, 03, 05, 06, 07, 09, 10, 13, 15, 51)
- [ ] API de catálogos (GET /catalogs/{number}/codes)
- [ ] Motor IGV: calcular gravado, exonerado, inafecto, exportación
- [ ] Motor IGV: operaciones gratuitas (códigos 11-16, 21, 31-36)
- [ ] ICBPER: cálculo de bolsas plásticas
- [ ] Tipo de cambio SUNAT automático (scrape o API)
- [ ] Cargos y descuentos (Cat. 53) globales y por ítem
- [ ] Tests unitarios del Tax Engine

### Sprint 4: XML UBL 2.1 — Factura (Semanas 7-8)
- [ ] Generar XML UBL 2.1 de Factura Electrónica completo
- [ ] Todos los campos obligatorios según guía SUNAT
- [ ] Firma digital XMLDSig con certificado X.509
- [ ] Validar XML contra reglas de SUNAT (validación pre-envío)
- [ ] Tests con XMLs de ejemplo de SUNAT
- [ ] Almacenar XML firmado en MinIO

---

## FASE 2 — EMISIÓN CORE (Sprints 5-8 | ~8 semanas)

### Sprint 5: SUNAT Gateway + CDR (Semanas 9-10)
- [ ] Cliente SOAP para webservice SUNAT
- [ ] Envío de factura a entorno beta
- [ ] Recepción y parsing de CDR (Constancia de Recepción)
- [ ] Clasificar respuesta: aceptada, rechazada, observada
- [ ] Almacenar CDR en MinIO
- [ ] NATS JetStream: stream "emission" con persistencia
- [ ] Worker que consume de NATS y envía a SUNAT
- [ ] Queue Group para load balancing de workers

### Sprint 6: Boleta + Resumen Diario (Semanas 11-12)
- [ ] XML UBL 2.1 de Boleta de Venta
- [ ] Acumulación de boletas del día
- [ ] Generación de XML de Resumen Diario
- [ ] Envío de Resumen Diario a SUNAT
- [ ] Manejo de Ticket asíncrono (consultar estado)
- [ ] CDR del Resumen Diario
- [ ] Automatización: job que genera resumen al final del día

### Sprint 7: Notas + Comunicación de Baja (Semanas 13-14)
- [ ] XML UBL 2.1 de Nota de Crédito (10 motivos Cat. 09)
- [ ] XML UBL 2.1 de Nota de Débito (3 motivos Cat. 10)
- [ ] Vinculación con documento original (serie + correlativo)
- [ ] XML de Comunicación de Baja
- [ ] Envío asíncrono + ticket + consulta de estado
- [ ] Validaciones de negocio: no anular boletas (van por resumen)

### Sprint 8: PDF + API REST Completa (Semanas 15-16)
- [ ] Generador de PDF (representación impresa)
- [ ] Template A4 y Ticket configurable por tenant
- [ ] Logo y colores del tenant en PDF
- [ ] Código QR en representación impresa
- [ ] API REST completa: todos los endpoints de documentos
- [ ] OpenAPI/Swagger auto-generado
- [ ] Paginación, filtros, ordenamiento en listados
- [ ] Rate limiting por plan

---

## FASE 3 — FRONTEND + WEBHOOKS (Sprints 9-11 | ~6 semanas)

### Sprint 9: Panel Web — Base (Semanas 17-18)
- [ ] Proyecto Next.js 16 con App Router
- [ ] Tailwind CSS + shadcn/ui
- [ ] Layout con sidebar, navbar, responsive
- [ ] Login / Registro
- [ ] Protección de rutas (middleware Next.js)
- [ ] Dashboard con KPIs: docs emitidos, aceptados, rechazados
- [ ] Gráficos con recharts

### Sprint 10: Panel Web — Emisión Manual (Semanas 19-20)
- [ ] Formulario de emisión de factura
- [ ] Formulario de emisión de boleta
- [ ] Formulario de nota de crédito (seleccionar doc original)
- [ ] Formulario de nota de débito
- [ ] Consulta y búsqueda de documentos
- [ ] Descarga de XML, PDF, CDR
- [ ] Configuración de empresa (datos, logo, series)

### Sprint 11: Webhooks + API Keys UI (Semanas 21-22)
- [ ] Servicio de webhooks con NATS
- [ ] Reintentos con backoff exponencial (3 intentos)
- [ ] Firma de payload con secret (HMAC)
- [ ] UI: gestión de webhooks en panel
- [ ] UI: gestión de API Keys (crear, ver, revocar)
- [ ] UI: página de uso del plan (docs emitidos vs límite)
- [ ] Cola de reintentos para envíos fallidos a SUNAT

---

## FASE 4 — IA + PULIDO (Sprints 12-14 | ~6 semanas)

### Sprint 12: Agente IA — Infraestructura (Semanas 23-24)
- [ ] Proyecto FastAPI con WebSocket
- [ ] BYOK router: OpenAI + Anthropic
- [ ] Configuración de API key por tenant (UI en panel)
- [ ] Knowledge base: reglas SUNAT, catálogos, errores CDR
- [ ] RAG básico sobre knowledge base
- [ ] Conexión read-only a PostgreSQL para consultas

### Sprint 13: Agente Emisor — Funcionalidades (Semanas 25-26)
- [ ] "¿Por qué me rechazaron esta factura?" → analiza CDR
- [ ] "Ayudame a emitir NC para factura X" → guía paso a paso
- [ ] "¿Cuántas boletas pendientes de resumen?" → consulta DB
- [ ] "¿Qué serie uso?" → explica reglas
- [ ] "¿Qué código de afectación IGV uso?" → busca en catálogo
- [ ] Chat embebido en panel web
- [ ] Historial de conversaciones

### Sprint 14: Testing + Hardening (Semanas 27-28)
- [ ] Tests de integración end-to-end
- [ ] Tests con entorno beta de SUNAT
- [ ] Validar TODOS los XMLs contra reglas oficiales
- [ ] Pruebas de carga (¿cuántos docs/min soporta?)
- [ ] Seguridad: OWASP top 10, SQL injection, XSS
- [ ] Documentación de API completa
- [ ] SDK de ejemplo en Python y JavaScript
- [ ] README y guía de deployment

---

## Resumen de Timeline

| Fase | Sprints | Semanas | Entregable |
|------|:-------:|:-------:|------------|
| 1 - Fundación | 1-4 | 1-8 | Infra + Auth + Tax + XML Factura |
| 2 - Emisión Core | 5-8 | 9-16 | SUNAT + Boleta + Notas + PDF + API |
| 3 - Frontend | 9-11 | 17-22 | Panel web + Webhooks |
| 4 - IA + Pulido | 12-14 | 23-28 | Agente IA + Testing + Docs |
| **TOTAL MVP** | **14** | **~28 semanas (~7 meses)** | **Producto completo listo** |

## Post-MVP (Plan de Mejoras)

| Fase | Contenido | Estimación |
|:----:|-----------|:----------:|
| 5 | GRE (Remitente + Transportista + Eventos) | 4-6 semanas |
| 6 | Detracciones + ISC + Retenciones/Percepciones | 4-6 semanas |
| 7 | SIRE Integration | 3-4 semanas |
| 8 | 5 Agentes IA adicionales | 6-8 semanas |
| 9 | White-label | 4-6 semanas |
| 10 | Factura negociable (factoring) | 4-6 semanas |
| 11 | App móvil | 6-8 semanas |
| 12 | ISO 27001 readiness | 8-12 semanas |
