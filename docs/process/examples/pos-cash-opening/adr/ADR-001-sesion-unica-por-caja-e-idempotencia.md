---
id: ADR-001
title: "Una sesión de caja abierta por caja: índice único parcial + idempotencia por client_mutation_id"
status: accepted
date: 2026-08-14
features: [CASH-FEAT-001, REPORT-FEAT-001]
supersedes: ""
superseded_by: ""
---

# ADR-001 — Una sesión de caja abierta por caja: índice único parcial + idempotencia por client_mutation_id

> **Ejemplo ilustrativo.** Decisión del ejemplo `pos-cash-opening`, no de Market Real. En particular, el transporte de `client_mutation_id` y el worker de sincronización siguen **pendientes de decisión** en `AGENTS.md`; este ADR los fija solo dentro del ejemplo.

## Contexto

- CASH-FEAT-001 exige una sola sesión abierta por caja (BR-CASH-001-01, REQ-002), también cuando dos dispositivos abren la misma caja a la vez (AC-CASH-001-03, respuesta de Q-CASH-001-01).
- La apertura funciona sin conexión y se reenvía desde el outbox del POS (REQ-005, BR-CASH-001-04): el mismo intento puede llegar varias veces.
- `TransactionBehavior` con "consultar y luego insertar" deja una ventana de carrera entre dos requests concurrentes.
- REPORT-FEAT-001 necesita conocer las aperturas sin leer el schema `cash_register` (lecturas cruzadas prohibidas por `backend-architecture`).
- Especificaciones: CASH-FEAT-001 (`Concurrencia y transacciones`, `Eventos y mensajería`) y REPORT-FEAT-001 (`Backend`).

## Decisión

1. Usamos un índice único parcial `ux_cash_sessions_active_register` sobre `cash_register.cash_sessions (cash_register_id)`, limitado a los estados que ocupan la caja (en 1.0.0, `Open`).
2. Usamos `client_mutation_id` (UUID generado por el POS una vez por intento de apertura y enviado en el body) con índice único; el handler devuelve la sesión ya registrada si llega de nuevo (replay idempotente con 200, sin volver a emitir eventos).
3. Traducimos la violación de `ux_cash_sessions_active_register` a 409 `CashSession.AlreadyOpen` con `UniqueConstraintErrorMap` en `Common.Infrastructure/Persistence`: cada módulo registra `restricción → Error`; una restricción no registrada sigue saliendo como 500.
4. Publicamos `CashSessionOpenedIntegrationEvent` con `IIntegrationEventPublisher`, y Reports lo consume con `IIntegrationEventConsumer<T>` e idempotencia por `cash_session_id`.
5. No usamos locks distribuidos ni `SELECT … FOR UPDATE`.

## Alternativas consideradas

| Alternativa | A favor | En contra | Motivo de descarte |
|---|---|---|---|
| Solo chequeo en el handler | Simple | Carrera entre dispositivos | No cumple AC-CASH-001-03 |
| Lock distribuido con Redis (`AddCommonRedis`) | Serializa aperturas | Dependencia en la ruta crítica del POS; expiraciones; no cubre escrituras fuera del API | Riesgo operativo sin ganancia sobre el índice |
| `SELECT … FOR UPDATE` sobre la fila de la caja | Garantía fuerte | Exige un agregado de caja en el módulo (fuera de alcance) y bloquea | No existe ese agregado |
| `sync_version` sobre un agregado de caja | Reutiliza un mecanismo existente | Responde `Concurrency.Conflict`, no `CashSession.AlreadyOpen`; en mobile dispara el flujo de conflicto, pendiente de decisión | No entrega el `code` que exige el AC |
| Header `Idempotency-Key` con tabla genérica | Reutilizable por todo el API | Middleware nuevo; transporte pendiente de decisión | Más alcance del necesario |
| Reports lee `cash_register` con Dapper | Sin evento | Lectura cruzada entre módulos | Prohibido por las reglas de módulos |

## Consecuencias

- Positivas: la unicidad la garantiza la base de datos; el `code` es estable para los clientes; los reenvíos del outbox son seguros.
- Negativas: la regla vive en el dominio y en la base de datos (duplicación deliberada); aparece un mecanismo nuevo en Common que requiere pruebas propias; si cambian los estados que ocupan la caja, el predicado del índice cambia con una migración.
- Obligaciones:
  - Todo command reintentable desde el POS lleva `client_mutation_id`.
  - Cada restricción registrada en `UniqueConstraintErrorMap` tiene una prueba de concurrencia de base de datos.
  - Todo consumer de negocio es idempotente por una clave natural además del inbox.

## Justificación de lo nuevo

### 1. ¿Qué problema resuelve?

Garantizar una sola sesión activa por caja ante carreras entre dispositivos y reenvíos, devolviendo un `code` de negocio, y comunicar la apertura a Reports sin acoplar módulos.

### 2. ¿Por qué no puede reutilizarse la arquitectura existente?

- `rg -n "23505|UniqueViolation" MarketjoyaBackend/src` → sin resultados: solo existe `DbUpdateConcurrencyExceptionHandler`, que mapea `sync_version` a `Concurrency.Conflict`.
- `rg -n "IIntegrationEventConsumer<" MarketjoyaBackend/src` → la infraestructura existe (`Common.Infrastructure/Messaging/Consumers`) pero no hay consumers de negocio: se reutiliza tal cual, lo nuevo es el primer uso entre módulos.
- `rg -n "client_mutation_id|ClientMutationId" MarketjoyaBackend/src` → sin uso en backend; en mobile existe `ClientMutationId` y `PendingSyncWriter`, que se reutilizan.

### 3. ¿Qué módulos participan?

`CashRegister` (productor, CASH), `Reports` (consumidor, REPORT), `Common.Infrastructure` (mapeo de restricciones) y `BuildingBlocks.Contracts` (evento). En mobile, `domain/sync` y `core/database`.

### 4. ¿Qué contratos utiliza?

- OpenAPI `CashRegister_OpenCashSession` (`POST /api/v1/cash-sessions`).
- Evento de integración `CashSessionOpenedIntegrationEvent` versión 1; exchange `marketjoya.CashSessionOpenedIntegrationEvent`; cola `marketjoya.Reports.CashSessionOpenedConsumer`.
- Tablas `cash_register.cash_sessions` (índices `ux_cash_sessions_active_register`, `ux_cash_sessions_client_mutation_id`) y `reports.cash_session_report_rows`.
- Puerto mobile `PendingSyncWriter`.

### 5. ¿Qué errores pueden producirse?

- `CashSession.AlreadyOpen` (CONFLICT, 409): caja ocupada, secuencial o por carrera.
- `Concurrency.Conflict` (CONFLICT, 409): dos envíos simultáneos con el mismo `client_mutation_id`.
- `OpenCashSession.Validation` (VALIDATION, 400).
- `Server.Failure` (FAILURE, 500): restricción no registrada o falla inesperada.
- `Network.Unavailable` / `Network.Timeout` (NETWORK) en el POS.
- Excepciones del consumer de Reports (sin `code` HTTP).

### 6. ¿Cómo se recupera de esos errores?

- POS: `NETWORK` y `FAILURE` → reintento con el mismo `client_mutation_id`, que termina en replay idempotente.
- POS: `Concurrency.Conflict` → en el ejemplo, un reintento con el mismo `client_mutation_id` (en Market Real el flujo de conflicto sigue pendiente de decisión).
- POS: `CashSession.AlreadyOpen` → operación rechazada y visible, nunca descartada en silencio.
- Consumer: reintentos de Wolverine (50 ms, 500 ms, 2 s; 30 s, 5 min, 30 min), luego cola de error en Postgres y `dead_letters` en Mongo, reproducibles; el upsert por `cash_session_id` hace seguro el replay.

### 7. ¿Cómo se monitorea?

- Traza `marketjoya.usecase.CashRegister.OpenCashSession` con `marketjoya.error.code`.
- Métricas `marketjoya_cash_register_sessions_opened_total`, `marketjoya_outbox_pending`, `marketjoya_sync_conflicts_total`.
- Alerta si la cola de error de `marketjoya.Reports.CashSessionOpenedConsumer` recibe mensajes; `correlation_id` para seguir la apertura del POS al reporte.

### 8. ¿Cómo se prueba?

- TEST-CASH-001-05 (`database`, concurrencia de dos aperturas).
- TEST-CASH-001-09 (`integration`, replay con el mismo `client_mutation_id`).
- TEST-CASH-001-08 (`messaging`, evento en el outbox y descarte con rollback).
- TEST-CASH-001-10 (`migration`, índices creados).
- TEST-REPORT-001-01 y TEST-REPORT-001-02 (`messaging`, consumer idempotente).

### 9. ¿Qué impacto tiene en otros features?

- CASH-FEAT-001: implementa la decisión.
- REPORT-FEAT-001: consume el evento y adopta la idempotencia por clave natural.
- AUTH-FEAT-001: sin cambios; provee el claim `cash_register`.
