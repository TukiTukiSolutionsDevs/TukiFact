# Capa: Application (UseCase + ViewModel)

## UseCase

Fakes de puertos (`FakePendingSyncWriter` de `:core:testing`; fakes de la feature para sus puertos). `runTest`.

Demuestra: éxito devuelve `AppResult.Success` y encola outbox con su `ClientMutationId` (`outbox.pendingFor(id)`); fallo de dominio devuelve `AppResult.Failure` y no encola; misma mutación ×2 = un efecto.

El UseCase no se instancia con el DAO. No uses `kotlin.Result` ni `assertThrows` para reglas predecibles.

Aserción de fallo: `type` y `code` del `AppError`, nunca `description`.

```kotlin
val failure = result as AppResult.Failure
assertEquals(ErrorType.NotFound, failure.error.type)
assertEquals("Sale.NotFound", failure.error.code)
```

Contrato: `../../backend-architecture/references/api-error-contract.md`. `AppResult`, `AppError` y `ErrorType` existen en `:domain` `error/`.

## ViewModel

`@get:Rule val main = MainDispatcherRule()` (usa `UnconfinedTestDispatcher` por defecto). Fake del UseCase o del puerto que devuelve `AppResult`. Turbine para la secuencia; `state.value` para snapshot.

```kotlin
vm.state.test {
    vm.onConfirm()
    assertEquals(SalesUiState.Loading, awaitItem())
    assertEquals(SalesUiState.Success(saleId), awaitItem())
}
```

Casos de error del ViewModel:

- `AppResult.Failure` → `UiState` con el mismo `AppError` (aserta `type` + `code`).
- `Validation` con `fieldErrors` → cada error llega a su campo.
- `Unauthorized` → dispara refresh una vez; si falla, efecto de logout (cuando exista el contrato de auth).

Debe haber collector si usas `stateIn`. Clases con `DispatcherProvider` reciben `TestDispatcherProvider(main.testDispatcher)`. No `Thread.sleep`.

## Worker de sync (cuando exista)

La decisión se testea en JVM, no en el worker: ya existe como `SyncAttemptPolicy` (`:domain` `sync/`) con `SyncAttemptPolicyTest`. Por contrato §6.7:

| Resultado del envío | Expectativa |
|---|---|
| `Success` | Operación `Synced` |
| `Network` (`Network.Unavailable` / `Network.Timeout`) | Sigue `Pending`, mismo `ClientMutationId` |
| `Network.RateLimited` (429) | Sigue `Pending`, mismo `ClientMutationId`; siguiente intento no antes de `AppError.retryAfterSeconds` (reloj fake); nunca rechazada |
| `Failure` (5xx, ej. 502 HTML) | Sigue `Pending`, mismo `ClientMutationId` |
| `Unauthorized` + refresh exitoso | Un reintento con el mismo `ClientMutationId` |
| `Unauthorized` + refresh fallido | Sigue `Pending` (nunca rechazada) |
| `Conflict` + `Concurrency.Conflict` | Entra al flujo de conflicto (protocolo: Pendiente de decisión); no se descarta |
| `Validation` / `Forbidden` / `NotFound` / otro `Conflict` / `Unknown` | Rechazada con el `AppError` guardado; sin reintento |

El worker solo llama y aplica backoff.
