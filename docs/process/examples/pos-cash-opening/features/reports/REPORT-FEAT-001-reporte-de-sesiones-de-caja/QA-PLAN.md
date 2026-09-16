---
feature: REPORT-FEAT-001
spec_version: 1.0.1
status: passed
last_run: 2026-09-29
updated: 2026-09-29
---

# Plan de QA — REPORT-FEAT-001 Reporte de sesiones de caja

> **Ejemplo ilustrativo.** Casos, rutas, resultados y evidencias son ficticios; forman parte del ejemplo `pos-cash-opening`. Versión 1.0.1: cambio inducido por CHG-001.

## Objetivo

Demostrar que cada sesión abierta aparece una sola vez en el reporte de su tienda (también si se abrió por aprobación de un supervisor), que el filtro por caja funciona y que solo los supervisores acceden.

## Alcance

US-REPORT-001-01 (AC-REPORT-001-01 a AC-REPORT-001-03) en backend (consumer, query, API) y front.

## Fuera de alcance

Exportación (Q-REPORT-001-01); la publicación del evento, que valida el plan de CASH-FEAT-001 (aquí se ejecuta como regresión).

## Criterios de entrada

- [x] Feature en ready-for-qa (G-QA)
- [x] CI verde en backend y front
- [x] CASH-FEAT-001 1.1.0 desplegado en QA con el umbral activo

## Criterios de salida

- [x] Todo TEST en pasa y toda fila de Regresión en pasa
- [x] EVID por cada TEST e2e
- [x] Ningún BUG critical/major abierto

## Entornos

| Entorno | URL o dispositivo | Versión / commit | Notas |
|---|---|---|---|
| CI backend | GitHub Actions, Testcontainers (Postgres, RabbitMQ) | backend `5e8a1f0` | Mensajería e integración |
| QA web | https://console.qa.ejemplo.invalid | front `3d9e6b2` | Chromium y Firefox (Playwright) |

## Datos de prueba

Supervisor `supervisor01` (tienda A, permiso `reports.cash-sessions.read`), `cajero01` (sin permiso); eventos `CashSessionOpenedIntegrationEvent` construidos con `CashSessionOpenedIntegrationEventBuilder` para "Caja 01" y "Caja 02" (tienda A) y "Caja 09" (tienda B); en 1.0.1, un evento con `OpenedAt` de aprobación (08:05) distinto de la hora de solicitud (07:58).

## Matriz de roles y permisos

| Rol | Acción | Esperado | TEST |
|---|---|---|---|
| Supervisor tienda A | Ver reporte | permitido, solo tienda A | TEST-REPORT-001-03 |
| Cajero | Ver reporte | denegado 403 y mensaje | TEST-REPORT-001-04 |
| Sin token | Ver reporte | denegado 401 | TEST-REPORT-001-04 |

## Casos de prueba

| ID | Criterios | Tipo | Nivel | Repo | Automatización | Resultado |
|---|---|---|---|---|---|---|
| TEST-REPORT-001-01 | AC-REPORT-001-01 | positivo | messaging | backend | tests/Marketjoya.Modules.Reports.IntegrationTests/CashSessions/CashSessionOpenedConsumerTests.cs | pasa |
| TEST-REPORT-001-02 | AC-REPORT-001-01 | recuperacion | messaging | backend | tests/Marketjoya.Modules.Reports.IntegrationTests/CashSessions/CashSessionOpenedConsumerTests.cs | pasa |
| TEST-REPORT-001-03 | AC-REPORT-001-01, AC-REPORT-001-02 | positivo | e2e | backend | tests/Marketjoya.Api.E2ETests/Reports/GetCashSessionReportEndpointTests.cs | pasa |
| TEST-REPORT-001-04 | AC-REPORT-001-03 | permisos | e2e | backend | tests/Marketjoya.Api.E2ETests/Reports/GetCashSessionReportEndpointTests.cs | pasa |
| TEST-REPORT-001-05 | AC-REPORT-001-02 | limite | integration | backend | tests/Marketjoya.Modules.Reports.IntegrationTests/CashSessions/GetCashSessionReportQueryTests.cs | pasa |
| TEST-REPORT-001-06 | AC-REPORT-001-01, AC-REPORT-001-03 | positivo | e2e | front | e2e/reports/cash-session-report.spec.ts | pasa |
| TEST-REPORT-001-07 | AC-REPORT-001-03 | error | contract | front | src/infrastructure/http/reports/cash-session-report.http-adapter.spec.ts | pasa |

Detalle: 01 el evento crea la fila (**1.0.1, modificada:** también un evento con `OpenedAt` de aprobación queda con esa hora); 02 el mismo evento dos veces deja una fila; 03 API con filtros y aislamiento por tienda; 04 401/403 con `traceId` y `correlationId`; 05 sesiones a las 23:59 y 00:00 de Lima caen en el día correcto; 06 Playwright del reporte y mensaje sin permiso; 07 fixtures ProblemDetails 400/403/500 → `AppError`.

## Regresión

| TEST | Feature | Motivo | Resultado |
|---|---|---|---|
| TEST-CASH-001-08 | CASH-FEAT-001 | El consumer depende de la forma del evento publicado por CASH | pasa |
| TEST-CASH-001-19 | CASH-FEAT-001 | CHG-001: el evento también se publica al aprobar una apertura | pasa |

## No funcionales

- Recuperación: TEST-REPORT-001-02 (mensajes repetidos).
- Seguridad: TEST-REPORT-001-04 y aislamiento por tienda en TEST-REPORT-001-03.
- Rendimiento, accesibilidad, migración: No aplica — volumen bajo; componentes del design system ya revisados; sin migraciones en 1.0.1.

## Evidencias

| ID | TEST | Archivo o enlace | Fecha |
|---|---|---|---|
| EVID-REPORT-001-01 | TEST-REPORT-001-03 | EVID-REPORT-001-01.md | 2026-08-31 |
| EVID-REPORT-001-02 | TEST-REPORT-001-04 | EVID-REPORT-001-02.md | 2026-08-31 |
| EVID-REPORT-001-03 | TEST-REPORT-001-06 | EVID-REPORT-001-03.md | 2026-08-31 |
| EVID-REPORT-001-04 | TEST-REPORT-001-03 | EVID-REPORT-001-04.md | 2026-09-29 |

## Resultados

| Fecha | Entorno | Build / commit | Ejecutados | Pasan | Fallan | Bloqueados | Responsable |
|---|---|---|---|---|---|---|---|
| 2026-09-29 | CI backend + QA web | backend `5e8a1f0`, front `3d9e6b2` | 7 + 2 regresión | 9 | 0 | 0 | qa (ejemplo) |
| 2026-08-31 | CI backend + QA web | backend `b7e41c2`, front `e52a9c7` | 7 + 1 regresión | 8 | 0 | 0 | qa (ejemplo) |
