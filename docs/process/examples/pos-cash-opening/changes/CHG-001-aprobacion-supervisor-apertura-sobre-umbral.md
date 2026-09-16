---
id: CHG-001
title: "Aprobación de supervisor para montos de apertura sobre umbral"
status: released
requested_by: "Jefatura de caja (ejemplo)"
date: 2026-09-14
features:
  - id: CASH-FEAT-001
    impact: direct
    bump: minor
  - id: REPORT-FEAT-001
    impact: induced
    via: CASH-FEAT-001
    bump: patch
requirements: [REQ-008]
released_in: REL-2026.10.1
---

# CHG-001 — Aprobación de supervisor para montos de apertura sobre umbral

> **Ejemplo ilustrativo.** Cambio ficticio del ejemplo `pos-cash-opening`: el umbral de S/ 500.00 y quién aprueba son respuestas confirmadas solo dentro del ejemplo. Recorre los 13 pasos de `docs/process/05-gestion-de-cambios.md`; cada sección indica el paso que la produce.

## Motivo

*Pasos 1–2 (analista, 2026-09-14).*

- Solicitante: jefatura de caja (en el ejemplo), al responder Q-PRD-002.
- Necesidad: las aperturas con más de S/ 500.00 concentran el riesgo de faltantes. CASH-FEAT-001 1.0.0, liberado en REL-2026.09.1, permite a cualquier cajero abrir con hasta S/ 5,000.00 sin control; la aprobación quedó fuera de alcance mientras el umbral no estaba confirmado.
- Si no se hace: el control sigue en papel y fuera del sistema; no hay registro de quién autorizó el efectivo inicial.

## Requisitos que cambian

*Paso 3 (analista, 2026-09-15). PRD 0.2.0.*

| Requisito | Antes | Después |
|---|---|---|
| REQ-008 (nuevo) | No existía; VIEW-002 sin requisito y Q-PRD-002 abierta | "Una apertura con monto inicial mayor a S/ 500.00 requiere la aprobación de un supervisor distinto del cajero antes de habilitar la venta" |
| REQ-002 (sin cambio de texto) | "Una caja tiene como máximo una sesión abierta a la vez" | Igual; BR-CASH-001-01 aclara que una apertura pendiente de aprobación también ocupa la caja |
| REQ-007 (métrica aclarada) | "usuario, caja, fecha-hora y monto" | Agrega "y aprobador, si aplica" |

## Impacto técnico

*Paso 4 (arquitecto, 2026-09-15). Checklist de `docs/process/04-trazabilidad-y-dependencias.md`.*

- Módulos directos: CASH (backend, mobile y front, que se suma a `repos`). Indirectos: REPORT (sin cambio de código; cambia cuándo llega el evento).
- Contratos OpenAPI:
  - `CashRegister_OpenCashSession`: la respuesta agrega `requiresApproval` (booleano, cambio aditivo).
  - Nuevos: `CashRegister_GetCashSession`, `CashRegister_GetPendingCashSessionApprovals`, `CashRegister_ApproveCashSessionOpening`, `CashRegister_RejectCashSessionOpening`.
- Eventos: `CashSessionOpenedIntegrationEvent` no cambia de forma (versión 1) pero ahora también se publica al aprobar, con `OpenedAt` = hora de aprobación. Nuevo evento de dominio interno `CashSessionApprovalRequestedDomainEvent`.
- Tablas y migraciones: `cash_register.cash_sessions` agrega `requested_at`, `decided_by`, `decided_at`; `opened_at` pasa a admitir nulos; el predicado de `ux_cash_sessions_active_register` pasa a `status IN ('Open', 'PendingApproval')` (migración `AddCashSessionApproval`; sin backfill porque las filas existentes son `Open`). ADR-001 sigue vigente: el índice cubre "los estados que ocupan la caja".
- Permisos: nuevo `cash-sessions.approve` para el rol supervisor.
- Concurrencia: dos supervisores decidiendo la misma apertura → `sync_version` → `Concurrency.Conflict`; la consola recarga y muestra `CashSession.NotPendingApproval`.
- Procesos: el POS consulta el estado de la apertura pendiente cada 5 s mientras la pantalla está visible (sin notificaciones push).
- Compatibilidad: una app POS 1.0.0 no entiende `requiresApproval`. El umbral se activa con la configuración `CashRegister:ApprovalThresholdEnabled` solo cuando el 100 % de los POS tiene la versión 1.1.0.
- Contrato de errores: nuevos `CashSession.NotFound`, `CashSession.NotPendingApproval`, `CashSession.SelfApprovalNotAllowed`.
- Reportes, notificaciones, integraciones externas: REPORT-FEAT-001 (inducido); sin notificaciones; sin integraciones externas.
- AUTH-FEAT-001: sin impacto; el permiso nuevo se asigna en la configuración de roles.

## Features afectados

*Paso 5 (arquitecto, 2026-09-15).*

| Feature | Impacto | Vía | Bump | Versión anterior → nueva | AC afectados |
|---|---|---|---|---|---|
| CASH-FEAT-001 | direct | — | minor | 1.0.0 → 1.1.0 | AC-CASH-001-01 (modificado), AC-CASH-001-04 (modificado), AC-CASH-001-06 (nuevo), AC-CASH-001-07 (nuevo), AC-CASH-001-08 (nuevo), AC-CASH-001-09 (nuevo) |
| REPORT-FEAT-001 | induced | CASH-FEAT-001 | patch | 1.0.0 → 1.0.1 | AC-REPORT-001-01 (modificado: aclara la hora de apertura de una sesión aprobada) |

## Plan de pruebas y regresión

*Paso 6 (QA, 2026-09-16).*

- CASH-FEAT-001, pruebas nuevas: TEST-CASH-001-17 (`domain`, umbral 500.00 / 500.01), TEST-CASH-001-18 (`integration`, apertura pendiente sin evento), TEST-CASH-001-19 (`integration`, aprobación publica el evento), TEST-CASH-001-20 (`integration`, rechazo libera la caja), TEST-CASH-001-21 (`e2e` backend, permisos y autoaprobación), TEST-CASH-001-22 (`e2e` front, consola de aprobación), TEST-CASH-001-23 (`integration` mobile, pendiente y bloqueo sin conexión), TEST-CASH-001-24 (`migration`).
- CASH-FEAT-001, pruebas modificadas: TEST-CASH-001-05 (la carrera incluye aperturas pendientes), TEST-CASH-001-14 (el flujo Maestro agrega una apertura de S/ 800.00 que queda pendiente).
- REPORT-FEAT-001, prueba modificada: TEST-REPORT-001-01 (evento publicado tras la aprobación).
- Regresión en CASH-FEAT-001: TEST-AUTH-001-01, TEST-REPORT-001-01, TEST-REPORT-001-03. Regresión en REPORT-FEAT-001: TEST-CASH-001-08, TEST-CASH-001-19.
- Se re-ejecuta la suite completa de ambos features.

## Evidencias

*Paso 11 (QA, 2026-09-25 a 2026-09-29).*

- CASH-FEAT-001: EVID-CASH-001-05 (TEST-CASH-001-21), EVID-CASH-001-06 (TEST-CASH-001-22), EVID-CASH-001-07 (TEST-CASH-001-14 modificada). Resultado: 24 TEST + 3 de regresión en `pasa` (QA-PLAN, fila del 2026-09-29).
- REPORT-FEAT-001: EVID-REPORT-001-04 (TEST-REPORT-001-03 con una sesión aprobada). Resultado: 7 TEST + 2 de regresión en `pasa`.
- Compatibilidad con POS 1.0.0 y el umbral apagado: verificada el 2026-09-28 (QA-PLAN de CASH-FEAT-001, `Resultados`).
- Ejecuciones de CI (ilustrativas): https://github.com/ejemplo/MarketjoyaBackend/actions/runs/1000000042 y https://github.com/ejemplo/MarketjoyaFront/actions/runs/3000000019.

## Decisión

*Paso 7 (2026-09-17).* **Aprobado** por la jefatura de caja (solicitante), el analista funcional y el arquitecto (en el ejemplo).

Condiciones:

- El umbral se activa en producción con `CashRegister:ApprovalThresholdEnabled` solo cuando el 100 % de los POS tenga la app 1.1.0.
- CASH-FEAT-001 1.1.0 y REPORT-FEAT-001 1.0.1 se liberan en la misma release.
- Sin cambios de forma en `CashSessionOpenedIntegrationEvent`; cualquier cambio de forma requiere otro CHG.

Registro de los 13 pasos:

| # | Paso | Fecha | Estado CHG |
|---|---|---|---|
| 1–2 | Solicitud y motivo | 2026-09-14 | proposed |
| 3–6 | Requisitos, impacto, features afectados, plan de pruebas | 2026-09-15 a 2026-09-16 | analysis |
| 7 | Decisión | 2026-09-17 | approved |
| 8 | Docs de cada feature actualizados (1.1.0 y 1.0.1, CHANGELOG `pendiente`); features `released` → `analysis` → `ready` | 2026-09-17 a 2026-09-18 | approved |
| 9 | Implementación con commits `feat(CHG-001)` / `feat(CASH-FEAT-001)` / `test(REPORT-FEAT-001)` | 2026-09-18 a 2026-09-23 | approved |
| 10 | Pruebas automatizadas en verde; features `ready-for-qa` | 2026-09-24 | implemented |
| 11 | Validación QA, regresión, evidencias | 2026-09-25 a 2026-09-29 | implemented |
| 12 | Aprobación de cada feature (CHANGELOG `aprobado`) | 2026-09-29 | verified |
| 13 | Release REL-2026.10.1: fila en `product/RELEASES.md`, `released_in` en CHG y features, `Liberado en` en ambos CHANGELOG | 2026-10-02 | released |
