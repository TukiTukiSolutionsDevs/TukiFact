---
feature: CASH-FEAT-001
spec_version: 1.1.0
status: passed
last_run: 2026-09-29
updated: 2026-09-29
---

# Plan de QA — CASH-FEAT-001 Abrir sesión de caja

> **Ejemplo ilustrativo.** Casos, rutas de automatización, entornos, resultados y evidencias son ficticios; muestran cómo QA diseña y registra la validación en el proceso de Market Real. Versión 1.1.0: incluye el diseño y la ejecución de pruebas de CHG-001.

## Objetivo

Demostrar que el cajero abre su caja con un monto válido, con y sin conexión, que nunca existen dos sesiones activas para la misma caja (reintentos, doble toque o dos dispositivos), que las aperturas mayores a S/ 500.00 solo se abren con la aprobación de un supervisor distinto del cajero, y que los errores llegan al usuario por su `code`.

## Alcance

- Historias US-CASH-001-01, US-CASH-001-02 y US-CASH-001-03 (AC-CASH-001-01 a AC-CASH-001-09).
- Plataformas: backend (API y mensajería), mobile (POS) y front (consola de aprobación, desde 1.1.0).
- Roles: cajero con y sin permiso, usuario sin caja, supervisor, usuario con permisos de cajero y supervisor.

## Fuera de alcance

- Aprobación sin conexión: no existe por ASM-CASH-001-04; solo se prueba el bloqueo en el POS.
- Consumo del evento por el reporte: lo valida el plan de REPORT-FEAT-001 (aquí se ejecuta como regresión).
- Impresión de comprobante (Q-CASH-001-02).

## Criterios de entrada

- [x] Feature en ready-for-qa (G-QA)
- [x] CI verde en backend, mobile y front
- [x] API desplegada en QA con `AddCashSessionApproval` aplicada y `CashRegister:ApprovalThresholdEnabled=true`
- [x] App POS 1.1.0 en emulador y POS físico de referencia; consola web de QA con la pantalla de aprobaciones
- [x] Usuarios, permisos y cajas de prueba cargados

## Criterios de salida

- [x] Todo TEST en pasa y toda fila de Regresión en pasa
- [x] EVID por cada TEST e2e y cada TEST manual
- [x] Ningún BUG critical/major abierto
- [x] p95 de confirmación de apertura < 2 s (REQ-006)

## Entornos

| Entorno | URL o dispositivo | Versión / commit | Notas |
|---|---|---|---|
| CI backend | GitHub Actions, Testcontainers (Postgres, RabbitMQ) | backend `5e8a1f0` | Unit, integración, mensajería, E2E HTTP |
| CI front | GitHub Actions, Playwright (Chromium, Firefox) | front `3d9e6b2` | Adapter y e2e |
| QA backend | https://api.qa.ejemplo.invalid | backend `5e8a1f0` | Umbral activo |
| QA web | https://console.qa.ejemplo.invalid | front `3d9e6b2` | Consola del supervisor |
| Emulador POS | Android API 34 | app-pos `8b27c44` | Maestro |
| POS físico de referencia | POS Android de QA (API 30) con red 4G | app-pos `8b27c44` | Rendimiento y compatibilidad con app 1.0.0 |

## Datos de prueba

- Usuarios: `cajero01` (caja "Caja 01", permiso `cash-sessions.open`), `cajero02` ("Caja 02", mismo permiso), `consulta01` (sin permisos), `sincaja01` (sesión sin caja), `supervisor01` (tienda A, `cash-sessions.approve`), `supervisor02` (tienda A, mismo permiso), `supervisor09` (tienda B), `mixto01` (ambos permisos, "Caja 03").
- Dispositivos: `pos-qa-01` y `pos-qa-02` asignados a "Caja 01" solo para concurrencia.
- Montos: 0.00, 200.00, 500.00, 5000.00 (válidos); 500.01 y 800.00 (requieren aprobación); 5000.01 y 10.005 (inválidos).
- Backend: builders `CashSessionBuilder` (con `.PendingApproval()`) y `OpenCashSessionCommandBuilder`; Postgres por colección de pruebas; sin datos personales reales.
- Mobile: `FakePendingSyncWriter` y `FakeCashSessionGateway` de `:core:testing`; sin conexión con `setAirplaneMode` de Maestro; red lenta con perfil de latencia de 800 ms.
- Front: fixtures ProblemDetails 403/404/409 para el adapter; usuarios de QA web para Playwright.

## Matriz de roles y permisos

| Rol | Acción | Esperado | TEST |
|---|---|---|---|
| Cajero con permiso | Abrir su caja | permitido | TEST-CASH-001-06 |
| Usuario sin permiso `cash-sessions.open` | Abrir caja | denegado 403; mensaje "No tienes permiso para abrir caja" | TEST-CASH-001-07 |
| Sin token | Abrir caja | denegado 401 | TEST-CASH-001-07 |
| Usuario con sesión sin caja | Abrir caja | rechazado 409 `CashSession.RegisterNotAssigned` | TEST-CASH-001-04 |
| Supervisor tienda A | Aprobar o rechazar una apertura pendiente de su tienda | permitido | TEST-CASH-001-22 |
| Cajero | Aprobar una apertura pendiente | denegado 403 | TEST-CASH-001-21 |
| Supervisor tienda B | Aprobar una apertura de la tienda A | 404 `CashSession.NotFound` | TEST-CASH-001-21 |
| Usuario con permisos de cajero y supervisor | Aprobar su propia apertura | 409 `CashSession.SelfApprovalNotAllowed` | TEST-CASH-001-21 |
| Supervisor | Abrir caja desde la consola web | no disponible (sin vista) | No aplica |

## Casos de prueba

| ID | Criterios | Tipo | Nivel | Repo | Automatización | Resultado |
|---|---|---|---|---|---|---|
| TEST-CASH-001-01 | AC-CASH-001-01 | positivo | domain | backend | tests/Marketjoya.Modules.CashRegister.UnitTests/CashSessions/CashSessionTests.cs | pasa |
| TEST-CASH-001-02 | AC-CASH-001-02 | limite | domain | backend | tests/Marketjoya.Modules.CashRegister.UnitTests/CashSessions/CashSessionTests.cs | pasa |
| TEST-CASH-001-03 | AC-CASH-001-02 | negativo | integration | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/CashSessions/OpenCashSessionTests.cs | pasa |
| TEST-CASH-001-04 | AC-CASH-001-03 | negativo | integration | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/CashSessions/OpenCashSessionTests.cs | pasa |
| TEST-CASH-001-05 | AC-CASH-001-03 | concurrencia | database | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/CashSessions/OpenCashSessionConcurrencyTests.cs | pasa |
| TEST-CASH-001-06 | AC-CASH-001-01, AC-CASH-001-03 | positivo | e2e | backend | tests/Marketjoya.Api.E2ETests/CashRegister/OpenCashSessionEndpointTests.cs | pasa |
| TEST-CASH-001-07 | AC-CASH-001-01 | permisos | e2e | backend | tests/Marketjoya.Api.E2ETests/CashRegister/OpenCashSessionEndpointTests.cs | pasa |
| TEST-CASH-001-08 | AC-CASH-001-01 | positivo | messaging | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/CashSessions/CashSessionOpenedPublicationTests.cs | pasa |
| TEST-CASH-001-09 | AC-CASH-001-05 | recuperacion | integration | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/CashSessions/OpenCashSessionIdempotencyTests.cs | pasa |
| TEST-CASH-001-10 | — | migracion | migration | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/Persistence/CashRegisterMigrationTests.cs | pasa |
| TEST-CASH-001-11 | AC-CASH-001-02, AC-CASH-001-03 | error | contract | mobile | core/network/src/test/kotlin/pe/marketjoya/core/network/cashregister/CashSessionHttpGatewayTest.kt | pasa |
| TEST-CASH-001-12 | AC-CASH-001-04 | positivo | integration | mobile | domain/src/test/kotlin/pe/marketjoya/domain/cashregister/OpenCashSessionUseCaseTest.kt | pasa |
| TEST-CASH-001-13 | AC-CASH-001-04 | recuperacion | database | mobile | core/database/src/androidTest/kotlin/pe/marketjoya/core/database/cashregister/RoomCashSessionLocalStoreTest.kt | pasa |
| TEST-CASH-001-14 | AC-CASH-001-01, AC-CASH-001-04, AC-CASH-001-05, AC-CASH-001-06 | positivo | e2e | mobile | maestro/cash-register/open-cash-session.yaml | pasa |
| TEST-CASH-001-15 | — | rendimiento | e2e | manual | manual: medición en POS físico con red 4G; no reproducible en emulador | pasa |
| TEST-CASH-001-16 | AC-CASH-001-05 | negativo | component | mobile | feature/cash-register/src/androidTest/kotlin/pe/marketjoya/feature/cashregister/OpenCashSessionScreenTest.kt | pasa |
| TEST-CASH-001-17 | AC-CASH-001-06 | limite | domain | backend | tests/Marketjoya.Modules.CashRegister.UnitTests/CashSessions/CashSessionApprovalTests.cs | pasa |
| TEST-CASH-001-18 | AC-CASH-001-06 | positivo | integration | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/CashSessions/OpenCashSessionTests.cs | pasa |
| TEST-CASH-001-19 | AC-CASH-001-07 | positivo | integration | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/CashSessions/ApproveCashSessionOpeningTests.cs | pasa |
| TEST-CASH-001-20 | AC-CASH-001-08 | positivo | integration | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/CashSessions/RejectCashSessionOpeningTests.cs | pasa |
| TEST-CASH-001-21 | AC-CASH-001-09 | permisos | e2e | backend | tests/Marketjoya.Api.E2ETests/CashRegister/CashSessionApprovalEndpointTests.cs | pasa |
| TEST-CASH-001-22 | AC-CASH-001-07, AC-CASH-001-08 | positivo | e2e | front | e2e/cash-register/approve-cash-session-opening.spec.ts | pasa |
| TEST-CASH-001-23 | AC-CASH-001-04, AC-CASH-001-06 | limite | integration | mobile | domain/src/test/kotlin/pe/marketjoya/domain/cashregister/OpenCashSessionUseCaseTest.kt | pasa |
| TEST-CASH-001-24 | — | migracion | migration | backend | tests/Marketjoya.Modules.CashRegister.IntegrationTests/Persistence/CashRegisterMigrationTests.cs | pasa |

Detalle de los casos:

- TEST-CASH-001-01: `CashSession.Open` con S/ 200.00 queda `Open` y emite `CashSessionOpenedDomainEvent`.
- TEST-CASH-001-02: 0.00 y 5000.00 son válidos; 5000.01 y 10.005 rompen `OpeningAmountMustBeInRangeRule`.
- TEST-CASH-001-03: vía `ISender`, 5000.01 → 400 `OpenCashSession.Validation` con `errors[0]` = `openingAmount` / `OpeningAmount.Invalid`.
- TEST-CASH-001-04: caja con sesión abierta → `CashSession.AlreadyOpen`; sesión sin caja → `CashSession.RegisterNotAssigned`.
- TEST-CASH-001-05: dos commands en paralelo para "Caja 01" → una fila activa, el otro `CashSession.AlreadyOpen`. **1.1.0 (modificada):** se repite con la primera apertura en `PendingApproval`.
- TEST-CASH-001-06: `POST /api/v1/cash-sessions` → 200; segunda apertura → 409 ProblemDetails con `code`, `traceId` y `correlationId`.
- TEST-CASH-001-07: sin token 401; sin permiso 403, ambos con `traceId` y `correlationId`.
- TEST-CASH-001-08: la apertura deja un `CashSessionOpenedIntegrationEvent` en el outbox; con rollback no queda ninguno.
- TEST-CASH-001-09: mismo `client_mutation_id` dos veces → misma `cashSessionId`, una sola fila, un solo evento.
- TEST-CASH-001-10: la migración crea el schema, la tabla y ambos índices únicos.
- TEST-CASH-001-11: fixtures ProblemDetails 400 y 409 → `AppError` con `fieldErrors` y `code`.
- TEST-CASH-001-12: sin conexión → apertura local `PendingSync` y una operación en el outbox con el mismo `ClientMutationId`.
- TEST-CASH-001-13: la apertura pendiente sobrevive al reinicio del proceso.
- TEST-CASH-001-14: Maestro — iniciar sesión, abrir con S/ 200.00 con doble toque; repetir sin conexión y reconectar. **1.1.0 (modificada):** apertura de S/ 800.00 que queda "Pendiente de aprobación".
- TEST-CASH-001-15: 30 aperturas en POS físico; p95 < 2 s. En 1.1.0 se re-midió con aperturas hasta S/ 500.00 (p95 1.5 s).
- TEST-CASH-001-16: agregado por BUG-001 — doble toque en `cash-opening-submit` produce una sola llamada al caso de uso con un único `ClientMutationId`.
- TEST-CASH-001-17: 500.00 queda `Open`; 500.01 queda `PendingApproval`; `Approve` con el mismo usuario rompe `SupervisorMustDifferFromCashierRule`.
- TEST-CASH-001-18: vía `ISender`, 800.00 → `requiresApproval = true`, fila `PendingApproval`, ningún evento de integración en el outbox.
- TEST-CASH-001-19: aprobar → `Open`, `decided_by` registrado y un `CashSessionOpenedIntegrationEvent` con `OpenedAt` = hora de aprobación.
- TEST-CASH-001-20: rechazar → `Rejected`; una apertura nueva de la misma caja se acepta.
- TEST-CASH-001-21: `POST …/approval` como cajero → 403; supervisor de otra tienda → 404 `CashSession.NotFound`; `mixto01` sobre su apertura → 409 `CashSession.SelfApprovalNotAllowed`; segunda decisión → 409 `CashSession.NotPendingApproval`.
- TEST-CASH-001-22: Playwright — `supervisor01` aprueba una pendiente y rechaza otra; `supervisor02` decide la misma a la vez y ve "Esta apertura ya fue decidida".
- TEST-CASH-001-23: caso de uso — con conexión y `requiresApproval` guarda `PendingApproval`; sin conexión y 500.01 devuelve `OpeningAmount.RequiresConnection` sin encolar; 500.00 sin conexión se encola.
- TEST-CASH-001-24: la migración `AddCashSessionApproval` recrea el índice con `PendingApproval` y conserva las filas `Open`.

## Regresión

| TEST | Feature | Motivo | Resultado |
|---|---|---|---|
| TEST-AUTH-001-01 | AUTH-FEAT-001 | La apertura toma la caja del claim `cash_register` emitido al iniciar sesión | pasa |
| TEST-REPORT-001-01 | REPORT-FEAT-001 | El consumer recibe el evento también al aprobar (CHG-001) | pasa |
| TEST-REPORT-001-03 | REPORT-FEAT-001 | El reporte muestra la sesión aprobada con la hora de aprobación | pasa |

## No funcionales

- Rendimiento (REQ-006): TEST-CASH-001-15 (p95 1.5 s en 1.1.0).
- Operación sin conexión y recuperación (REQ-005): TEST-CASH-001-12, TEST-CASH-001-13, TEST-CASH-001-09, TEST-CASH-001-23.
- Concurrencia: TEST-CASH-001-05, TEST-CASH-001-16 y la decisión simultánea de TEST-CASH-001-22.
- Seguridad: TEST-CASH-001-07 y TEST-CASH-001-21; la caja no se acepta desde el body (revisado en TEST-CASH-001-06).
- Auditoría (REQ-007): columnas de auditoría en TEST-CASH-001-01 y TEST-CASH-001-06; aprobador en TEST-CASH-001-19.
- Migración: TEST-CASH-001-10 y TEST-CASH-001-24.
- Accesibilidad: mensajes como texto, no solo color (TEST-CASH-001-14 y TEST-CASH-001-22).
- Compatibilidad: emulador API 34, POS físico API 30, Chromium y Firefox. Verificado manualmente el 2026-09-28 que una app POS 1.0.0 sigue abriendo cajas con `CashRegister:ApprovalThresholdEnabled=false` (ver fila de `Resultados`).

## Evidencias

| ID | TEST | Archivo o enlace | Fecha |
|---|---|---|---|
| EVID-CASH-001-01 | TEST-CASH-001-06 | EVID-CASH-001-01.md | 2026-08-31 |
| EVID-CASH-001-02 | TEST-CASH-001-07 | EVID-CASH-001-02.md | 2026-08-31 |
| EVID-CASH-001-03 | TEST-CASH-001-14 | EVID-CASH-001-03.md | 2026-08-31 |
| EVID-CASH-001-04 | TEST-CASH-001-15 | EVID-CASH-001-04.md | 2026-08-31 |
| EVID-CASH-001-05 | TEST-CASH-001-21 | EVID-CASH-001-05.md | 2026-09-29 |
| EVID-CASH-001-06 | TEST-CASH-001-22 | EVID-CASH-001-06.md | 2026-09-29 |
| EVID-CASH-001-07 | TEST-CASH-001-14 | EVID-CASH-001-07.md | 2026-09-29 |

## Resultados

| Fecha | Entorno | Build / commit | Ejecutados | Pasan | Fallan | Bloqueados | Responsable |
|---|---|---|---|---|---|---|---|
| 2026-09-29 | CI backend + CI front + emulador + QA web + POS físico | backend `5e8a1f0`, app-pos `8b27c44`, front `3d9e6b2` | 24 + 3 regresión | 27 | 0 | 0 | qa (ejemplo) |
| 2026-09-28 | POS físico con app 1.0.0, umbral apagado | backend `5e8a1f0`, app-pos `d41f7e2` | 1 (compatibilidad) | 1 | 0 | 0 | qa (ejemplo) |
| 2026-08-31 | CI backend + emulador + POS físico | backend `b7e41c2`, app-pos `d41f7e2` | 16 + 1 regresión | 17 | 0 | 0 | qa (ejemplo) |
| 2026-08-27 | CI backend + emulador POS | backend `b7e41c2`, app-pos `c90d3a4` | 14 | 13 | 1 (TEST-CASH-001-14 → BUG-001) | 0 | qa (ejemplo) |
