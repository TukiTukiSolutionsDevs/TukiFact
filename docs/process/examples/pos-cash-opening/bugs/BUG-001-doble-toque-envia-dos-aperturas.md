---
id: BUG-001
title: "Doble toque en Abrir caja envía dos aperturas"
feature: CASH-FEAT-001
severity: major
status: closed
source: qa
found_in: "app-pos c90d3a4 (build pos-qa del 2026-08-26)"
tests: [TEST-CASH-001-14, TEST-CASH-001-16]
criteria: [AC-CASH-001-05]
date: 2026-08-27
---

# BUG-001 — Doble toque en Abrir caja envía dos aperturas

> **Ejemplo ilustrativo.** Incidencia ficticia del ejemplo `pos-cash-opening`: muestra cómo QA registra una desviación contra la fuente de verdad sin tocar el AC.

## Descripción

Con red lenta, un doble toque en "Abrir caja" envía dos aperturas con identificadores de operación distintos: la primera abre la caja y la segunda recibe `CashSession.AlreadyOpen`, que el POS muestra como error aunque la caja quedó abierta. Sin conexión, la segunda apertura queda en el outbox y termina rechazada al sincronizar. Afecta a cualquier cajero en redes lentas.

## Pasos para reproducir

1. Emulador Android API 34 con app-pos `c90d3a4` contra QA backend; red limitada (latencia 800 ms).
2. Iniciar sesión con `cajero01` ("Caja 01" sin sesión abierta).
3. Ingresar S/ 200.00 y tocar dos veces "Abrir caja" en menos de 300 ms (paso `doubleTapOn` de TEST-CASH-001-14).

## Resultado esperado

AC-CASH-001-05 y BR-CASH-001-04: "Caja 01" queda con una sola sesión abierta y el POS muestra la caja abierta **sin mensaje de error**.

## Resultado obtenido

- En el servidor hay una sola sesión (el índice único lo garantiza), pero el POS muestra "La caja ya tiene una sesión abierta" (`type` CONFLICT, `code` `CashSession.AlreadyOpen`, `correlationId` `pos-qa-01-7f3a`).
- El log del adapter muestra dos requests con `clientMutationId` distinto.
- Sin conexión: dos operaciones en el outbox; la segunda queda `Rejected`.

## Fuente de verdad

- **FEATURE**: AC-CASH-001-05 exige "sin mensaje de error" y BR-CASH-001-04 define el reenvío idempotente. La TECHNICAL-SPEC (sección `Mobile`) exige un `ClientMutationId` por intento de apertura.
- **PRD**: REQ-005 pide aperturas sin duplicados, pero no describe el doble toque; no contradice al FEATURE.
- **Comportamiento previo**: no existía (feature nuevo).
- Conclusión: la especificación es clara y la implementación no la cumple → BUG (no `Q`, no `CHG`). Severidad `major`: AC incumplido con alternativa (el cajero ignora el mensaje), sin pérdida de dinero ni de datos.

## Evidencia

- Ejecución fallida de TEST-CASH-001-14 (2026-08-27): captura y log en el artefacto de CI https://github.com/ejemplo/MarketjoyaMobile/actions/runs/2000000007 (ilustrativo).
- Re-ejecución tras la corrección: EVID-CASH-001-03 (2026-08-31, tres ejecuciones consecutivas en verde).

## Resolución

- Causa raíz: `OpenCashSessionViewModel` generaba `ClientMutationId.random()` dentro de `onSubmit()`, de modo que cada toque creaba una operación nueva; el botón se deshabilitaba recién al llegar el primer estado.
- Corrección: el `ClientMutationId` se genera al iniciar el intento de apertura y solo se renueva tras un resultado final; el botón se deshabilita en el mismo evento del toque.
- Commit: `fix(BUG-001): genera ClientMutationId una vez por intento de apertura` (MarketjoyaMobile `d41f7e2`); PR `fix(BUG-001): evitar doble apertura por doble toque`.
- Prueba agregada: TEST-CASH-001-16 (`component`, doble toque envía una sola apertura).
- Verificación de QA (2026-08-31): TEST-CASH-001-14 y TEST-CASH-001-16 en `pasa` sobre app-pos `d41f7e2`; estado `verified`. Se cierra con la release que incluye el fix.
