# Changelog — REPORT-FEAT-001 Reporte de sesiones de caja

> **Ejemplo ilustrativo.** Historial de versiones del feature relacionado del ejemplo `pos-cash-opening`.

## [1.0.1] — 2026-09-17

| Campo | Valor |
|---|---|
| Cambio | CHG-001 |
| Tipo | patch |
| Descripción | Aclara que una sesión abierta por aprobación de supervisor aparece con la hora de aprobación y que las aperturas pendientes o rechazadas no aparecen; sin cambio de código de producción |
| Motivo | Cambio inducido por CASH-FEAT-001 1.1.0 (CHG-001) |
| Responsable | analista y qa (ejemplo) |
| Requisitos | REQ-004 (sin cambio) |
| Criterios afectados | AC-REPORT-001-01 (modificado: aclaración del Given) |
| Archivos técnicos | backend (solo pruebas): tests/Marketjoya.Modules.Reports.IntegrationTests/CashSessions/CashSessionOpenedConsumerTests.cs |
| Pruebas | TEST-REPORT-001-01 (modificada); regresión TEST-CASH-001-08, TEST-CASH-001-19 |
| Features relacionados | CASH-FEAT-001 (origen) |
| Riesgos | Interpretación de la hora de apertura en sesiones aprobadas |
| Validación | aprobado |
| Liberado en | REL-2026.10.1 |

## [1.0.0] — 2026-08-11

| Campo | Valor |
|---|---|
| Cambio | — |
| Tipo | initial |
| Descripción | Creación del feature |
| Motivo | Alta inicial desde el PRD (REQ-004) |
| Responsable | analista (ejemplo) |
| Requisitos | REQ-004 |
| Criterios afectados | AC-REPORT-001-01 a AC-REPORT-001-03 (nuevos) |
| Archivos técnicos | backend: src/Modules/Reports/**, openapi/marketjoya-api-v1.json; front: src/core/reports/*, src/data/reports/*, src/infrastructure/http/reports/*, src/ui/reports/cash-session-report/*, e2e/reports/cash-session-report.spec.ts |
| Pruebas | TEST-REPORT-001-01 a TEST-REPORT-001-07 (nuevas) |
| Features relacionados | CASH-FEAT-001 (productor del evento) |
| Riesgos | Cambio de forma del evento de CASH |
| Validación | aprobado |
| Liberado en | REL-2026.09.1 |
