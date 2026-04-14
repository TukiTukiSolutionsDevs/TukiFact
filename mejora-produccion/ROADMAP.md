# TukiFact — Roadmap de Mejora para Producción

> Plan completo para llevar TukiFact de "funcional" a "competitivo y superior" vs Nubefact/PSE.
> Investigado: 2026-04-13 | Fuentes: SUNAT oficial, cpe.sunat.gob.pe, Tavily Research

---

## Resumen Ejecutivo

```
BATCH A ████████████████████████ 100%  "Sin esto no vendés" — ✅ BACKEND COMPLETO (2026-04-13)
BATCH B ░░░░░░░░░░░░░░░░░░░░░░░  0%   "Competitivo con Nubefact"
BATCH C ░░░░░░░░░░░░░░░░░░░░░░░  0%   "MEJOR que Nubefact"
BATCH D ░░░░░░░░░░░░░░░░░░░░░░░  0%   "Deploy + Infraestructura"
```

---

## BATCH A — "Sin esto no vendés" (BLOQUEANTE)

> **Prioridad**: 🔴 CRÍTICA — Sin estas funcionalidades NO se puede vender a clientes reales.
> **Estimación**: 3-4 sprints (cada sprint = 2-3 días intensivos con IA)

### A1. Guía de Remisión Electrónica (GRE) 🔴
**Spec**: `01-gre-guia-remision.md`
**Complejidad**: ALTA | **Estimación**: 1.5 sprints

| Tarea | Detalle |
|-------|---------|
| A1.1 | Entity `DespatchAdvice` + EF Config + Migration |
| A1.2 | Catálogo 20 (Motivos de traslado) — seed data |
| A1.3 | UBIGEO completo (1874 distritos) — tabla + seed |
| A1.4 | XML Builder GRE-R (DespatchAdvice UBL 2.1) |
| A1.5 | XML Builder GRE-T (igual estructura, diferente emisor) |
| A1.6 | Cliente REST SUNAT GRE (NO es SOAP, es REST API con OAuth2) |
| A1.7 | OAuth2 token service para GRE (client_id/client_secret vía SOL) |
| A1.8 | DespatchAdviceController (CRUD + envío) |
| A1.9 | Frontend: página emisión GRE (formulario dinámico) |
| A1.10 | Frontend: lista GRE + detalle + PDF |
| A1.11 | GRE por Eventos (complementa GRE existente) |

**Dato clave investigado**: La GRE usa **REST API** (no SOAP como facturas). Requiere credenciales OAuth2 generadas en menú SOL de SUNAT. Token expira en 1 hora.

### A2. SUNAT Producción Real 🔴
**Spec**: `02-sunat-produccion.md`
**Complejidad**: MEDIA | **Estimación**: 0.5 sprint

| Tarea | Detalle |
|-------|---------|
| A2.1 | Configurar URLs de producción SOAP (sendBill, sendSummary, getStatus) |
| A2.2 | Configurar URL producción REST para GRE |
| A2.3 | Configurar URL retención/percepción |
| A2.4 | Switch beta/producción por tenant (config en DB) |
| A2.5 | Manejo de certificado digital real (.pfx) por tenant |
| A2.6 | Credenciales SOL por tenant (para GRE REST API) |
| A2.7 | Proceso de homologación SUNAT (set de pruebas) |
| A2.8 | Logs de envío/respuesta SUNAT para debugging |

**URLs investigadas**:
- Factura Beta: `https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService`
- Factura Prod: `https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService`
- GRE Token: `https://api-seguridad.sunat.gob.pe/v1/clientessol/{client_id}/oauth2/token/`
- GRE Envío: `https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/{numRuc}-09-{serie}-{correlativo}`
- Retención/Percepción: URL separada en producción
- Consulta CDR: endpoint separado

### A3. Email Automático al Cliente 🔴
**Spec**: `08-email-notificaciones.md`
**Complejidad**: BAJA | **Estimación**: 0.5 sprint

| Tarea | Detalle |
|-------|---------|
| A3.1 | Servicio de email abstracto (IEmailService) |
| A3.2 | Implementación Resend (recomendado por simplicidad y precio) |
| A3.3 | Template HTML para envío de comprobante (factura/boleta/NC/ND) |
| A3.4 | Adjuntar PDF al email |
| A3.5 | Configuración de email por tenant (SMTP propio o Resend compartido) |
| A3.6 | NATS consumer para envío asíncrono (no bloquear emisión) |
| A3.7 | Frontend: toggle "enviar por email al emitir" en config |
| A3.8 | Registro de envíos (email_logs) |

### A4. Validar RUC + Autocompletar 🟡
**Spec**: `06-tipo-cambio-ruc.md`
**Complejidad**: BAJA | **Estimación**: 0.25 sprint

| Tarea | Detalle |
|-------|---------|
| A4.1 | Servicio consulta RUC (APIs: apis.net.pe o peruapi.com) |
| A4.2 | Endpoint `/v1/utils/validate-ruc/{ruc}` |
| A4.3 | Servicio consulta DNI |
| A4.4 | Endpoint `/v1/utils/validate-dni/{dni}` |
| A4.5 | Frontend: autocompletar al escribir RUC/DNI en emisión y clientes |
| A4.6 | Cache de consultas (evitar hits repetidos) |

**APIs investigadas**:
- `apis.net.pe` — API REST, token auth, responde RUC con razón social, estado, condición, dirección, ubigeo
- `peruapi.com` — Similar, incluye DNI y tipo de cambio
- Respuesta incluye: razón social, estado (ACTIVO), condición (HABIDO), dirección, ubigeo

### A5. Forgot / Reset Password 🟡
**Complejidad**: BAJA | **Estimación**: 0.25 sprint

| Tarea | Detalle |
|-------|---------|
| A5.1 | Endpoint `POST /v1/auth/forgot-password` (genera token + envía email) |
| A5.2 | Endpoint `POST /v1/auth/reset-password` (valida token + cambia password) |
| A5.3 | Entity `PasswordResetToken` (token, userId, expiresAt, usedAt) |
| A5.4 | Template email "Restablecer contraseña" |
| A5.5 | Frontend: páginas forgot-password y reset-password |

**Dependencia**: A3 (Email service)

---

## BATCH B — "Competitivo con Nubefact"

> **Prioridad**: 🟡 ALTA — Estas funcionalidades las tiene todo PSE/OSE serio.
> **Estimación**: 3-4 sprints

### B1. Tipo de Cambio SUNAT Automático
**Spec**: `06-tipo-cambio-ruc.md`
**Complejidad**: BAJA | **Estimación**: 0.25 sprint

| Tarea | Detalle |
|-------|---------|
| B1.1 | Servicio consulta tipo de cambio SBS/SUNAT |
| B1.2 | Endpoint `GET /v1/utils/exchange-rate?date=YYYY-MM-DD` |
| B1.3 | Cache diario (1 consulta por día) |
| B1.4 | Scheduler: actualizar tipo de cambio automáticamente cada día |
| B1.5 | Tabla `exchange_rates` (date, buy, sell, currency, source) |
| B1.6 | Frontend: mostrar tipo de cambio en emisión cuando currency=USD |

**API investigada**: `apis.net.pe/v2/sunat/tipo-cambio` o scraping directo de SBS

### B2. ICBPER (Impuesto Bolsas Plásticas)
**Spec**: `04-icbper.md`
**Complejidad**: BAJA | **Estimación**: 0.25 sprint

| Tarea | Detalle |
|-------|---------|
| B2.1 | Campo `icbperQuantity` en DocumentItem |
| B2.2 | Cálculo automático: cantidad × S/ 0.50 (desde 2023, Ley 30884 art.12) |
| B2.3 | Nodo XML: TaxCategory con código tributo 7152 (OTH) |
| B2.4 | No forma parte de base imponible IGV |
| B2.5 | Frontend: campo "Cantidad de bolsas" en formulario de emisión |
| B2.6 | PDF: mostrar ICBPER desglosado |

**Dato investigado**: S/ 0.50 por bolsa desde 2023+. Código tributo 7152, tipo internacional OTH.

### B3. Detracciones (SPOT)
**Spec**: `03-detracciones-spot.md`
**Complejidad**: ALTA | **Estimación**: 1 sprint

| Tarea | Detalle |
|-------|---------|
| B3.1 | Tabla `detraction_codes` — Catálogo 54 completo (40+ códigos) |
| B3.2 | Seed data con todos los códigos y porcentajes vigentes |
| B3.3 | Campo `detractionCode`, `detractionPercent`, `detractionAmount` en Document |
| B3.4 | Nodos XML: PaymentMeans (código detracción + cuenta BN) |
| B3.5 | Nodos XML: Leyenda 2006 "Operación sujeta a detracción" |
| B3.6 | Nodos XML: PaymentTerms con monto y porcentaje |
| B3.7 | Validación: detracción solo aplica si monto > S/ 700 (servicios) o según bien |
| B3.8 | Frontend: selector de código detracción + cálculo automático |
| B3.9 | Frontend: campo cuenta Banco de la Nación del proveedor |
| B3.10 | PDF: mostrar información de detracción |

**Datos investigados**:
- Códigos actualizados 2025-2026 (R.S. 086-2025, 121-2025, 175-2025)
- Servicios generales: 12% (código 037)
- Transporte carga: 4% (código 027)
- Construcción: 4% (código 030)
- Intermediación laboral: 12% (código 012)
- Leyendas XML: códigos 3000-3009

### B4. Consulta Validez CPE
**Complejidad**: MEDIA | **Estimación**: 0.5 sprint

| Tarea | Detalle |
|-------|---------|
| B4.1 | Cliente REST para API de consulta integrada SUNAT |
| B4.2 | OAuth2 token (mismas credenciales SOL que GRE) |
| B4.3 | Endpoint `GET /v1/utils/validate-cpe` |
| B4.4 | Frontend: página de consulta de validez |
| B4.5 | Verificar estado de documentos propios emitidos |

**URL investigada**: API REST SUNAT de consulta integrada (requiere credenciales de API SUNAT generadas en SOL)

### B5. Rate Limiting por Plan
**Complejidad**: MEDIA | **Estimación**: 0.5 sprint

| Tarea | Detalle |
|-------|---------|
| B5.1 | Middleware de rate limiting por tenant |
| B5.2 | Configuración por plan (Free=50/mes, Emprendedor=300/mes, etc.) |
| B5.3 | Headers de respuesta: X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset |
| B5.4 | Contador en Redis o PostgreSQL |
| B5.5 | Endpoint `GET /v1/usage` — uso actual del plan |
| B5.6 | Frontend: barra de uso en dashboard |
| B5.7 | Alertas al 80% y 100% de uso |

### B6. Catálogos SUNAT API
**Complejidad**: MEDIA | **Estimación**: 0.5 sprint

| Tarea | Detalle |
|-------|---------|
| B6.1 | Tabla `sunat_catalogs` + `sunat_catalog_codes` |
| B6.2 | Seed data: Cat. 01, 02, 03, 05, 06, 07, 09, 10, 12, 13, 15, 20, 51, 52, 53, 54 |
| B6.3 | Endpoints CRUD catálogos |
| B6.4 | Versionamiento de catálogos (SUNAT actualiza periódicamente) |
| B6.5 | Frontend: página de consulta de catálogos |
| B6.6 | Usar catálogos en formularios de emisión (dropdowns dinámicos) |

---

## BATCH C — "MEJOR que Nubefact" (Diferenciadores)

> **Prioridad**: 🟢 MEDIA — Estas funcionalidades nos hacen SUPERIORES a la competencia.
> **Estimación**: 4-5 sprints

### C1. Retenciones Electrónicas (tipo 20)
**Spec**: `07-retenciones-percepciones.md`
**Complejidad**: ALTA | **Estimación**: 1 sprint

| Tarea | Detalle |
|-------|---------|
| C1.1 | Entity `RetentionDocument` + EF Config |
| C1.2 | XML Builder Retención (UBL 2.0 — diferente a facturas) |
| C1.3 | Serie R001+ |
| C1.4 | Porcentaje: 3% del IGV |
| C1.5 | Cliente SOAP retención (endpoint separado) |
| C1.6 | RetentionsController |
| C1.7 | Frontend: emisión + lista retenciones |

### C2. Percepciones Electrónicas (tipo 40)
**Spec**: `07-retenciones-percepciones.md`
**Complejidad**: ALTA | **Estimación**: 1 sprint

| Tarea | Detalle |
|-------|---------|
| C2.1 | Entity `PerceptionDocument` + EF Config |
| C2.2 | XML Builder Percepción (UBL 2.0) |
| C2.3 | Serie P001+ |
| C2.4 | Porcentajes: 0.5%, 1%, 2% según caso (Cat. 22) |
| C2.5 | Cliente SOAP percepción |
| C2.6 | PerceptionsController |
| C2.7 | Frontend: emisión + lista percepciones |

**URL producción**: Endpoint Retención/Percepción separado del de facturas

### C3. SIRE Integración
**Spec**: `05-sire-integracion.md`
**Complejidad**: ALTA | **Estimación**: 1.5 sprints

| Tarea | Detalle |
|-------|---------|
| C3.1 | OAuth2 client para API SIRE SUNAT |
| C3.2 | Servicio generar RVIE (Registro Ventas) desde documentos emitidos |
| C3.3 | Servicio aceptar/rechazar propuesta SUNAT |
| C3.4 | Servicio importar ajustes posteriores |
| C3.5 | Servicio descargar propuesta SUNAT |
| C3.6 | SireController (endpoints para gestión SIRE) |
| C3.7 | Frontend: página SIRE con flujo completo |
| C3.8 | Scheduler: generar SIRE automáticamente cada mes |

**Dato investigado**:
- Obligatorio desde enero 2025 para Régimen General (no PRICOS)
- PRICOS postergado a junio 2026 (R.S. 392-2025/SUNAT)
- API REST con token OAuth2
- NO consumir desde cliente web (CORS bloqueado) — siempre desde backend
- Servicios TUS (resumable uploads) deben ser en JAVA según SUNAT, pero se puede usar HTTP estándar desde .NET

### C4. Multi-moneda (USD/EUR)
**Complejidad**: MEDIA | **Estimación**: 0.5 sprint

| Tarea | Detalle |
|-------|---------|
| C4.1 | Campo currency ya existe en Document — validar soporte completo |
| C4.2 | Integrar tipo de cambio automático en emisión USD |
| C4.3 | Nodo XML: `cbc:DocumentCurrencyCode` + tipo de cambio en `cac:PaymentExchangeRate` |
| C4.4 | Cálculos en moneda original + equivalente PEN |
| C4.5 | Frontend: selector moneda + tipo de cambio auto |
| C4.6 | PDF: mostrar ambas monedas |

### C5. Facturación Recurrente (DIFERENCIADOR — Nubefact NO lo tiene)
**Complejidad**: MEDIA | **Estimación**: 1 sprint

| Tarea | Detalle |
|-------|---------|
| C5.1 | Entity `RecurringInvoice` (template + schedule) |
| C5.2 | Scheduler: generar documentos automáticamente según frecuencia |
| C5.3 | Frecuencias: diario, semanal, quincenal, mensual, anual |
| C5.4 | RecurringInvoicesController |
| C5.5 | Frontend: CRUD facturas recurrentes + historial |
| C5.6 | Email notificación de factura recurrente generada |
| C5.7 | Pausa/cancelación de recurrencia |

### C6. Cotizaciones → Factura (DIFERENCIADOR)
**Complejidad**: MEDIA | **Estimación**: 0.5 sprint

| Tarea | Detalle |
|-------|---------|
| C6.1 | Entity `Quotation` (borrador de documento) |
| C6.2 | QuotationsController (CRUD + convertir a factura) |
| C6.3 | Frontend: crear cotización → enviar por email → aprobar → emitir factura |
| C6.4 | PDF cotización (sin valor tributario) |
| C6.5 | Estado: borrador, enviada, aprobada, facturada, cancelada |

---

## BATCH D — Deploy + Infraestructura

> **Prioridad**: 🔵 DESPUÉS de Batch A y B — infraestructura para ir a producción real.
> **Estimación**: 2-3 sprints

### D1. SSL/Certbot + DNS
| Tarea | Detalle |
|-------|---------|
| D1.1 | Contratar VPS (DigitalOcean/Hetzner recomendado) |
| D1.2 | DNS tukifact.net.pe → IP del VPS |
| D1.3 | Certbot en Docker (Let's Encrypt) |
| D1.4 | nginx.conf con SSL activado |
| D1.5 | Auto-renovación certificado |
| D1.6 | Redirect HTTP → HTTPS |

### D2. CI/CD Pipeline
| Tarea | Detalle |
|-------|---------|
| D2.1 | GitHub Actions: build + test + lint |
| D2.2 | GitHub Actions: deploy a VPS (SSH + docker compose) |
| D2.3 | Secrets en GitHub (JWT_SECRET, PG_PASSWORD, etc.) |
| D2.4 | Ambientes: staging + production |
| D2.5 | Rollback automático si health check falla |

### D3. Backup Strategy
| Tarea | Detalle |
|-------|---------|
| D3.1 | pg_dump automático (cron diario) |
| D3.2 | MinIO backup (sincronizar a S3 o storage externo) |
| D3.3 | Rotación de backups (mantener 30 días) |
| D3.4 | Script de restore probado |
| D3.5 | Backup antes de cada deploy |

### D4. Monitoring
| Tarea | Detalle |
|-------|---------|
| D4.1 | Health check dashboard |
| D4.2 | Alertas por email si un servicio cae |
| D4.3 | Métricas de uso (requests/sec, latencia, errores) |
| D4.4 | Log aggregation básico |

---

## Orden de Ejecución Recomendado

```
SEMANA 1-2  → A1: GRE Remitente + Transportista (lo más complejo primero)
SEMANA 2    → A2: SUNAT Producción real (activar endpoints reales)
SEMANA 3    → A3: Email automático + A5: Forgot password
SEMANA 3    → A4: Validar RUC + autocompletar
SEMANA 4    → B2: ICBPER + B1: Tipo de cambio (rápidos)
SEMANA 4-5  → B3: Detracciones SPOT (complejo)
SEMANA 5    → B4: Consulta validez CPE + B5: Rate limiting
SEMANA 5-6  → B6: Catálogos SUNAT
SEMANA 6-7  → C1+C2: Retenciones + Percepciones
SEMANA 7-8  → C3: SIRE integración
SEMANA 8    → C4: Multi-moneda + C5: Facturación recurrente
SEMANA 9    → C6: Cotizaciones + D1: SSL/DNS
SEMANA 10   → D2: CI/CD + D3: Backups + D4: Monitoring
```

## Archivos que se Crearán/Modificarán (Estimación)

### Nuevas Entities (Domain)
- `DespatchAdvice.cs` — GRE
- `DespatchAdviceItem.cs` — Items de GRE
- `Ubigeo.cs` — Tabla UBIGEO
- `DetractionCode.cs` — Catálogo 54
- `ExchangeRate.cs` — Tipo de cambio diario
- `RetentionDocument.cs` — Retención
- `PerceptionDocument.cs` — Percepción
- `RecurringInvoice.cs` — Factura recurrente
- `Quotation.cs` + `QuotationItem.cs` — Cotizaciones
- `PasswordResetToken.cs` — Reset password
- `EmailLog.cs` — Log de emails
- `SunatCatalog.cs` + `SunatCatalogCode.cs` — Catálogos

### Nuevos Controllers (API)
- `DespatchAdviceController.cs` — GRE CRUD + envío
- `RetentionsController.cs` — Retenciones
- `PerceptionsController.cs` — Percepciones
- `RecurringInvoicesController.cs` — Facturación recurrente
- `QuotationsController.cs` — Cotizaciones
- `SireController.cs` — SIRE
- `CatalogsController.cs` — Catálogos SUNAT
- `UtilsController.cs` — RUC, DNI, tipo de cambio, validar CPE

### Nuevos Services (Infrastructure)
- `GreSunatClient.cs` — REST client para GRE
- `SunatProductionClient.cs` — SOAP client producción
- `SireClient.cs` — REST client para SIRE
- `EmailService.cs` (Resend impl)
- `RucValidationService.cs`
- `ExchangeRateService.cs`
- `CpeValidationService.cs`
- `GreXmlBuilder.cs` — XML builder para GRE

### Nuevas Páginas Frontend
- `/gre/new` — Emisión GRE
- `/gre` — Lista GRE
- `/gre/[id]` — Detalle GRE
- `/retentions/new` — Emisión retención
- `/retentions` — Lista retenciones
- `/perceptions/new` — Emisión percepción
- `/perceptions` — Lista percepciones
- `/recurring` — Facturas recurrentes
- `/quotations` — Cotizaciones
- `/quotations/new` — Nueva cotización
- `/sire` — Gestión SIRE
- `/catalogs` — Catálogos SUNAT
- `/forgot-password` — Olvidé mi contraseña
- `/reset-password` — Restablecer contraseña

---

## Métricas Post-Implementación

| Métrica | Actual | Post-Batch A | Post-Batch B | Post-Batch C |
|---------|:------:|:------------:|:------------:|:------------:|
| Controllers | 18 | 20 | 23 | 28 |
| Entities | 17 | 22 | 26 | 32 |
| Endpoints API | 27+ | 40+ | 55+ | 75+ |
| Tipos documento | 6 | 9 | 9 | 11 |
| Rutas frontend | 17 | 22 | 28 | 40+ |
| vs Nubefact | 60% | 80% | 95% | 110% |

---

_Este roadmap se actualiza al completar cada batch. Cada spec técnica está en su archivo correspondiente._
