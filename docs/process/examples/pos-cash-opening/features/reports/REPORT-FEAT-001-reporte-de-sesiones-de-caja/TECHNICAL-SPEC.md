---
feature: REPORT-FEAT-001
spec_version: 1.0.1
status: reviewed
adrs: [ADR-001]
impact:
  modules_direct: [REPORT]
  modules_indirect: [CASH]
  features_affected: []
  regression_tests: [TEST-CASH-001-08, TEST-CASH-001-19]
  contracts: ["openapi:Reports_GetCashSessionReport", "event:CashSessionOpenedIntegrationEvent"]
  migrations: true
updated: 2026-09-17
---

# Especificación técnica — REPORT-FEAT-001 Reporte de sesiones de caja

> **Ejemplo ilustrativo.** El módulo `Reports`, su consumer, tablas y pantallas no existen en Market Real; son el diseño del ejemplo, alineado con las skills `backend-architecture` y `hexaclean-architecture`. Versión 1.0.1: cambio inducido por CHG-001, sin cambios de código de producción.

## Resumen

Módulo backend `Reports` (schema `reports`): `CashSessionOpenedConsumer` alimenta el read model `cash_session_report_rows` desde `CashSessionOpenedIntegrationEvent`, y `GetCashSessionReportQuery` (Dapper) lo expone en `GET /api/v1/reports/cash-sessions` (`Reports_GetCashSessionReport`). Pantalla Angular de reporte. Repos: backend y front.

**1.0.1 (CHG-001, inducido):** CASH-FEAT-001 1.1.0 publica el mismo evento también al aprobar una apertura, con `OpenedAt` = hora de aprobación. El consumer no cambia; cambian la documentación (AC-REPORT-001-01, BR-REPORT-001-02) y un caso de TEST-REPORT-001-01.

## Arquitectura actual involucrada

- backend: `src/Common/Marketjoya.Common.Infrastructure/Messaging/` (`IIntegrationEventConsumer`, inbox de Wolverine, reintentos y cola de error), `Persistence/` (`ReadDbConnection`, `AddReadDbConnection`), `src/Common/Marketjoya.Common.Presentation/Endpoints/` (`IEndpoint`, `WithStandardProblems`), `ICurrentUser.CompanyIds`, `IClock.LimaNow`.
- front: `src/base/go-result.type.ts`, `src/infrastructure/http/to-app-error.mapper.ts`, `src/ui/app.routes.ts`.
- Evento consumido: definido en la especificación de CASH-FEAT-001.

## Reutilización

- Consumo con inbox durable de Wolverine (deduplicación por id de mensaje) — reutilizado.
- Lectura con `ReadDbConnection` + Dapper — reutilizado.
- `toAppError` y `GoResult` en el front — reutilizados; componentes `MrCard`, `MrMoney`, `MrStatusChip` del design system.
- Descartado: leer el schema `cash_register` desde Reports (lecturas cruzadas entre módulos prohibidas).
- Búsqueda de consumers previos: `rg -n "IIntegrationEventConsumer<" MarketjoyaBackend/src` → ninguno de negocio; este es el primero (ADR-001).
- 1.0.1: se reutiliza el consumer tal cual; no se agregan campos al evento.

## Componentes nuevos

- Módulo backend `Reports` (4 proyectos, schema `reports`).
- Primer consumer de negocio entre módulos: `CashSessionOpenedConsumer` (ADR-001).
- 1.0.1: ninguno.

## Backend

- `Marketjoya.Modules.Reports.Infrastructure/Messaging/CashSessionOpenedConsumer.cs`: `public sealed class CashSessionOpenedConsumer : IIntegrationEventConsumer<CashSessionOpenedIntegrationEvent>`; traduce a `RecordCashSessionOpenedCommand` y lo envía con `ISender` (sin reglas de negocio).
- `RecordCashSessionOpenedCommand` → `CashSessionReportRow` con upsert por `cash_session_id` (BR-REPORT-001-03, además del inbox). Repositorio `ICashSessionReportRowRepository`: `GetByCashSessionIdAsync`, `AddAsync`.
- `GetCashSessionReportQuery(DateOnly Date, Guid? CashRegisterId) : IRequest<Result<IReadOnlyList<CashSessionReportItem>>>`; filtra por `company_id` en `ICurrentUser.CompanyIds` (BR-REPORT-001-01) y convierte el día de Lima a rango UTC usando `opened_at` (BR-REPORT-001-02).
- Endpoint `GET /reports/cash-sessions`: `.RequireAuthorization("reports.cash-sessions.read")`, `.WithName("GetCashSessionReport")`, `.Produces<IReadOnlyList<CashSessionReportItem>>()`, `.WithStandardProblems()`.
- 1.0.1: sin cambios de código de producción.

## Frontend

- `src/core/reports/port/in/get-cash-session-report.port.ts`, `src/core/reports/port/out/cash-session-report-reader.port.ts`, `src/core/reports/application/use-case/get-cash-session-report.use-case.ts` (`GoResult<CashSessionReport, AppError>`).
- `src/data/reports/cash-session-report.dto.ts`; `src/infrastructure/http/reports/cash-session-report.http-adapter.ts` (único traductor con `toAppError`).
- `src/ui/reports/cash-session-report/cash-session-report.screen.ts`; ruta `reports/cash-sessions` protegida por permiso; mensaje por `code`.
- 1.0.1: sin cambios.

## Mobile

No aplica — el reporte es solo web.

## Base de datos y migraciones

- Schema `reports`, tabla `cash_session_report_rows`: `cash_session_id uuid pk`, `company_id`, `cash_register_id`, `opening_amount numeric(9,2)`, `currency`, `opened_at timestamptz`; índice `(company_id, opened_at desc)`.
- Migración EF `CreateCashSessionReportRows` con `--context ReportsDbContext`.
- 1.0.1: sin migraciones nuevas.

## Contratos

- `openapi:Reports_GetCashSessionReport` — `GET /api/v1/reports/cash-sessions?date=YYYY-MM-DD&cashRegisterId=uuid`. Consumidor: front.
- Consume `event:CashSessionOpenedIntegrationEvent` versión 1 (sin cambio de forma en CHG-001).

## Eventos y mensajería

- Cola `marketjoya.Reports.CashSessionOpenedConsumer`, enlazada al exchange `marketjoya.CashSessionOpenedIntegrationEvent`.
- Inbox durable + upsert por `cash_session_id`: un mensaje repetido no duplica filas.
- Fallas: reintentos 50 ms / 500 ms / 2 s, luego 30 s / 5 min / 30 min, luego cola de error (Postgres + `dead_letters` en Mongo con `correlation_id`).
- 1.0.1: el evento llega también al aprobar una apertura en CASH; nunca llega por aperturas pendientes o rechazadas. TEST-REPORT-001-01 agrega ese caso; regresión con TEST-CASH-001-19.

## Integraciones externas

No aplica — sin integraciones externas.

## Seguridad y autorización

- Permiso `reports.cash-sessions.read` asignado al rol supervisor; sin permiso 403.
- Filtro por `company` del token: un supervisor nunca ve otra tienda.

## Observabilidad

- Trazas `marketjoya.usecase.Reports.RecordCashSessionOpened` y `marketjoya.usecase.Reports.GetCashSessionReport`; el consumer restaura `marketjoya.correlation_id` del envelope.
- Retraso del read model visible con `marketjoya_sync_lag_seconds` (prevista); alerta si la cola de error recibe mensajes.

## Errores

| code | type | HTTP |
|---|---|---|
| GetCashSessionReport.Validation | VALIDATION | 400 |
| (sin `code`) | FORBIDDEN | 403 |

## Concurrencia y transacciones

- El consumer procesa un mensaje por transacción; upsert por clave primaria evita duplicados ante entregas repetidas.
- La query es de solo lectura, sin transacción.

## Compatibilidad y datos existentes

- 1.0.0: tabla nueva sin datos previos.
- 1.0.1: filas existentes no cambian; las sesiones aprobadas después de activar el umbral en CASH llegan con la hora de aprobación.
- Si el evento sube de versión, el consumer debe aceptar la versión 1 hasta que no queden mensajes.

## Despliegue y reversión

- 1.0.0: migración de `reports` antes del rollout; API y front después. Reversión: ocultar la ruta en el front; el consumer puede seguir llenando el read model.
- 1.0.1: sin despliegue propio; se valida junto con CASH-FEAT-001 1.1.0 en la misma release.

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Retraso del read model mayor a 1 minuto | baja | medio | Alerta de cola; ASM-REPORT-001-02 acepta hasta 1 minuto |
| Cambio de forma del evento en CASH | media | medio | Regresión TEST-CASH-001-08 y TEST-REPORT-001-01 en cada cambio de CASH |
| 1.0.1: el supervisor interpreta mal la hora de sesiones aprobadas | baja | bajo | BR-REPORT-001-02 aclarada; texto de ayuda en la pantalla (sin cambio de código en esta versión) |

## Análisis de impacto

- Módulos directos: REPORT. Indirectos: CASH (productor del evento).
- Endpoints: `Reports_GetCashSessionReport`, solo front. Tablas: schema propio `reports`.
- Eventos: consume `CashSessionOpenedIntegrationEvent`; en 1.0.1 cambia cuándo llega, no su forma.
- Procesos programados, notificaciones, integraciones externas: No aplica.
- Permisos: `reports.cash-sessions.read`.
- Regresión: TEST-CASH-001-08 (publicación al abrir) y TEST-CASH-001-19 (publicación al aprobar).
- Rendimiento: índice por empresa y fecha; volumen esperado bajo (decenas de cajas por tienda).

## Archivos a crear o modificar

- backend: `src/Modules/Reports/Marketjoya.Modules.Reports.Infrastructure/Messaging/CashSessionOpenedConsumer.cs`; `src/Modules/Reports/Marketjoya.Modules.Reports.Application/CashSessions/RecordCashSessionOpened/RecordCashSessionOpenedCommand.cs`, `…Handler.cs`; `src/Modules/Reports/Marketjoya.Modules.Reports.Application/CashSessions/GetCashSessionReport/GetCashSessionReportQuery.cs`, `…Handler.cs`; `src/Modules/Reports/Marketjoya.Modules.Reports.Presentation/CashSessions/GetCashSessionReport/GetCashSessionReportEndpoint.cs`; `src/Modules/Reports/Marketjoya.Modules.Reports.Infrastructure/Migrations/<timestamp>_CreateCashSessionReportRows.cs`; `openapi/marketjoya-api-v1.json`
- front: `src/core/reports/…`, `src/data/reports/cash-session-report.dto.ts`, `src/infrastructure/http/reports/cash-session-report.http-adapter.ts`, `src/ui/reports/cash-session-report/cash-session-report.screen.ts`, `src/ui/app.routes.ts`, `e2e/reports/cash-session-report.spec.ts`
- 1.0.1 — backend (solo pruebas): `tests/Marketjoya.Modules.Reports.IntegrationTests/CashSessions/CashSessionOpenedConsumerTests.cs`

## Orden de implementación

1. backend — migración y consumer idempotente (TEST-REPORT-001-01, TEST-REPORT-001-02).
2. backend — query y endpoint (TEST-REPORT-001-05, TEST-REPORT-001-03, TEST-REPORT-001-04).
3. front — adapter y caso de uso (TEST-REPORT-001-07).
4. front — pantalla y e2e (TEST-REPORT-001-06).
5. QA — regresión TEST-CASH-001-08.
6. 1.0.1 — agregar el caso "evento tras aprobación" a TEST-REPORT-001-01 y ejecutar regresión TEST-CASH-001-19 cuando CASH 1.1.0 esté en QA.
