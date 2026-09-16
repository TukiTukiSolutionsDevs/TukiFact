# Data layer y offline-first

```text
UI -> UseCase -> Port -> Local/Remote adapters -> Mapper -> Domain
```

- El puerto expone dominio, no Room ni Retrofit.
- Local y Remote son internos de Infrastructure.
- Mappers explícitos (ej. `PendingSyncOperationMapper`). Transacción Room cuando el invariante local lo pide.
- Errores HTTP → `AppError` solo vía `HttpErrorMapper` (`references/backend-coupling.md`).
- `MarketjoyaDatabase` (`:core:database`, versión 2) exporta schema a `core/database/schemas/`; cada nueva versión lleva migración explícita en `MarketjoyaMigrations` (hoy `MIGRATION_1_2`) y su caso en `MarketjoyaMigrationsTest`.

## Outbox local (implementado)

| Pieza | Módulo | Contrato |
|---|---|---|
| `LocalId`, `ClientMutationId` | `:domain` `sync/SyncIdentifiers.kt` | UUID canónico; `random()` / `parse()` |
| `PendingSyncOperation` | `:domain` `sync/` | `localId`, `clientMutationId`, `kind` (nombre del Command), `status` `Pending`/`Synced`/`Conflict`/`Rejected`; `rejection` (`AppError`) presente solo si `Rejected`; `reject(error)` solo desde `Pending`; `isRemovable` solo si `Synced` |
| `PendingSyncWriter` (puerto) | `:domain` `sync/` | `enqueue` idempotente por `clientMutationId`; `existsByMutationId`; `markRejected(clientMutationId, error)` (no-op si la mutación no existe o ya no está `Pending`) |
| `SyncAttemptPolicy` | `:domain` `sync/` | `decide(result, sessionRefresh)` → `SyncAttemptDecision`: `Acknowledge`, `Retry(notBeforeSeconds)`, `RefreshSessionThenRetry`, `KeepPending`, `ConflictFlow`, `Reject(error)` |
| `PendingSyncOperationEntity` | `:core:database` `outbox/` | Tabla `pending_sync_operations`, PK `client_mutation_id`, índice `local_id`; desde v2, 8 columnas `rejection_*` con el `AppError` completo |
| `RoomPendingSyncWriter` | `:core:database` `outbox/` | Insert `IGNORE`: reintento = no-op. Registrado en `databaseModule` |
| `FakePendingSyncWriter` + `PendingSyncWriterContract` | `:core:testing` `sync/` | Mismo contrato para fake (JVM) y Room (`androidTest`) |

Reglas:

- Toda mutación sincronizable tiene `LocalId` y `ClientMutationId`.
- No borrar hasta ACK del Command.
- Conflicto por entidad, visible en UI (no silencioso).

## Resultado del envío

Contrato §6.7 (`../../backend-architecture/references/api-error-contract.md`). El envío de una operación del outbox termina en `AppResult`; `SyncAttemptPolicy` implementa esta tabla (`SyncAttemptPolicyTest`) y el worker que la aplica aún no existe:

| Resultado | Acción sobre la operación |
|---|---|
| `Success` | ACK: pasa a `Synced`; recién ahí es removible |
| `Network` (`Network.Unavailable` / `Network.Timeout`, incluido 408) | Queda `Pending`; se reintenta |
| `Network.RateLimited` (429) | Queda `Pending`; siguiente intento no antes de `AppError.retryAfterSeconds`, mismo `ClientMutationId`; nunca rechazada |
| `Failure` (5xx, incluidos 502/503/504) | Queda `Pending`; se reintenta |
| `Unauthorized` | Refresh de sesión y un reintento; si el refresh falla, queda `Pending` hasta restaurar la sesión (nunca rechazada) |
| `Conflict` con `code = Concurrency.Conflict` | Flujo de conflicto del protocolo de sync (Pendiente de decisión); no se descarta; visible en UI |
| `Validation`, `Forbidden`, `NotFound`, otro `Conflict` o `Unknown` | Rechazada: se guarda el `AppError` completo (`type`, `code`, `fieldErrors`, `correlationId`); no se reintenta |

- Reintentar solo operaciones idempotentes: toda operación del outbox lleva `ClientMutationId`; sin él no se reintenta.
- El reintento reenvía el mismo `ClientMutationId`; nunca genera uno nuevo.
- El worker no decide por status ni por `description`: ramifica por `type` y luego por `code`.

## Pendiente de decisión

- Payload por operación (hoy la fila solo guarda `kind`).
- Si `client_mutation_id` viaja como header o en el body del Command.
- Worker de sync (WorkManager está en el catálogo, no cableado) y backoff del reintento.
- Refresh de sesión (endpoints y protocolo; `references/backend-coupling.md`).
- Protocolo de conflicto de sync (`Concurrency.Conflict`).
- Puerto de lectura del outbox y marcado de ACK/conflicto.
- Si POS vende offline.

## Preventa

Pedido persistido local antes de sync. Cartera, productos, precios y rutas viven local. Stock offline es informativo.

## POS

Pago electrónico y facturación pueden requerir online. Venta offline: Pendiente de decisión.

## Background

| Pregunta | Mecanismo |
|---|---|
| ¿Solo mientras la UI vive? | Coroutine con `DispatcherProvider` |
| ¿Debe sobrevivir process death? | WorkManager (no cableado aún) |
| ¿Continuo y visible de verdad? | Evaluar Foreground Service |

No `GlobalScope` (detekt `GlobalCoroutineUsage`). No services permanentes por periférico.
