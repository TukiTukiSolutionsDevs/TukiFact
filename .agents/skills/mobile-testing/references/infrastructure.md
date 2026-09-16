# Capa: Infrastructure

## Room

- Mapper Room ↔ dominio: test JVM de round-trip + un caso de dato corrupto (ej. `PendingSyncOperationMapperTest`).
- DAO/adapter: `androidTest` con una base in-memory por clase. Aserta la fila de tu `ClientMutationId` / `LocalId`.
- Si existe fake del mismo puerto: el test Room extiende el contrato de `:core:testing` (`RoomPendingSyncWriterTest : PendingSyncWriterContract()`), así fake y Room prueban lo mismo.
- Migraciones: un test por versión en `MarketjoyaMigrationsTest` (`androidTest`, schemas de `core/database/schemas/`); hoy cubre `MIGRATION_1_2` (filas existentes intactas y fila migrada rechazable).

## HTTP

MockWebServer 5.5 (`mockwebserver3.MockWebServer`, `MockResponse.Builder()`) de la clase, con `server.takeRequest()` en cada test para no desalinear la cola.

Cubre hoy (`:core:network`):

- ProblemDetails → `AppError` según la tabla de casos mínimos (manda el status, `code` del cuerpo, `fieldErrors`, `Retry-After` con reloj fijo, cuerpo HTML o no JSON, timeout y conexión cerrada) — `HttpErrorMapperTest`.
- `X-Correlation-Id` añadido si falta, preservado si existe y conservado en fallos sin respuesta (contrato §6.3) — `NetworkSuiteTest`.

### Casos mínimos del mapper (contrato §7)

Fuente: `../../backend-architecture/references/api-error-contract.md`. Fixtures JSON reales de ProblemDetails; aserta `type`, `code`, `status`, `traceId`, `correlationId` del `AppError`, nunca `description`.

| Respuesta servida | `AppError` esperado |
|---|---|
| 400 con `code` y `errors` | `Validation`, `code` del cuerpo, `fieldErrors` con `field` + `code` |
| 409 con `code = BusinessRule.<Regla>` | `Conflict`, `code` del cuerpo |
| 409 con `code = Concurrency.Conflict` | `Conflict`, `Concurrency.Conflict` |
| 401 sin `code` | `Unauthorized`, `Auth.Unauthorized` |
| 403 sin `code` | `Forbidden`, `Auth.Forbidden` (no `Failure`) |
| 404 sin `code` / con `code` | `NotFound`, `Http.NotFound` / `code` del cuerpo |
| 500 sin `code` | `Failure`, `Server.Failure` |
| 400 sin `code` | `Validation`, `Http.BadRequest` |
| 409 sin `code` | `Conflict`, `Http.Conflict` (outbox: rechazada, no flujo de conflicto) |
| 401 sin cuerpo | `Unauthorized`, `Auth.Unauthorized` |
| 408 | `Network`, `Network.Timeout` |
| 429 texto plano con `Retry-After` | `Network`, `Network.RateLimited`, `retryAfterSeconds` = valor del header |
| 429 / 503 con `Retry-After` como fecha HTTP | `retryAfterSeconds` convertido a segundos (reloj fijo en el test) |
| 502 con cuerpo HTML de proxy | `Failure`, `Server.Failure`, `status = 502` |
| Status fuera de §2 (ej. 418) con cuerpo no JSON | `Unknown`, `Unknown.Unexpected`, `status` preservado, `description = ""` |
| 422 con `code` en el cuerpo | `Unknown`, `code` del cuerpo conservado |
| ProblemDetails sin `detail` | `description` = `title`; sin ambos, cadena vacía |
| Conexión cerrada sin respuesta (efecto de socket de MockWebServer) | `Network`, `Network.Unavailable` |
| Timeout de lectura (`SocketTimeoutException`) | `Network`, `Network.Timeout` |

- `traceId` y `correlationId` llegan al `AppError`.
- Error `Network` (sin respuesta o timeout) conserva el `correlationId` generado: aserta que es igual al header `X-Correlation-Id` de `server.takeRequest()` (o del request enviado cuando no hubo conexión).
- Manda el status (§6.1): cuerpo ausente, no JSON o sin `code` con status de §2 → ese `type` + código reservado; `Unknown` solo con status fuera de §2.
- Timeout: cliente OkHttp de test con `readTimeout` corto y respuesta retrasada con `headersDelay`/`bodyDelay` del servidor; nada de `Thread.sleep`.

Por feature: Command `POST /api/v1/...` con `ClientMutationId` (header o body: Pendiente de decisión), 409 `Concurrency.Conflict` → `AppResult.Failure` con ese `code`.

No mockees la interfaz Retrofit con MockK si puedes servir JSON real: el bug vive en el mapper.

## DI

Cada app tiene `di/<App>ModulesTest` con `module { includes(posModules) }.verify(extraTypes = listOf(Context::class))`. Un módulo Koin nuevo se valida ahí.

## Hardware

Pendiente de decisión (puertos no definidos). Cuando existan: `UsbManager` no entra al test; fake del puerto en `:core:testing`; parser de bytes con test unitario de fixture, no USB en CI.

## WorkManager

No cableado. Cuando exista el worker de sync: la decisión de qué sincronizar es UseCase JVM; el worker lleva un instrumentado corto con `work-testing`.
