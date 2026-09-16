# Refresh de sesión

Operaciones protegidas: ante un `AppError` con `type === 'UNAUTHORIZED'` (401; `code` del backend o `Auth.Unauthorized` si llegó sin `code`), un executor refresca la sesión (single-flight) y reintenta **una** vez. Si el refresh falla, cierre de sesión ([contrato §6.5](../../backend-architecture/references/api-error-contract.md)).

```text
operación protegida -> AppError UNAUTHORIZED
 -> RefreshUseCaseExecutor
 -> RefreshSessionUseCase (single-flight)
 -> SessionRefresherPort.refresh   (adapter: no existe)
 -> error del refresh: se devuelve sin reintentar -> cierre de sesión
 -> éxito: reintenta la operación una vez y devuelve su resultado
```

El executor decide solo por `type`; nunca por `description` ni por el status crudo. `FORBIDDEN` no dispara refresh.

## Implementado (`MarketjoyaFront/src/core/auth/`)

| Pieza | Archivo | Comportamiento |
|---|---|---|
| `RefreshSessionPort` (in) | `port/in/refresh-session.port.ts` | `execute(): Promise<GoResult<true, AppError>>` |
| `SessionRefresherPort` (out) | `port/out/session-refresher.port.ts` | `refresh(): Promise<GoResult<true, AppError>>`; endpoint, payload y duración viven solo en el adapter |
| `RefreshSessionUseCase` | `application/use-case/refresh-session.use-case.ts` | Llamadas concurrentes comparten una promesa en vuelo; al resolverse se libera y un refresh posterior vuelve a llamar al puerto |
| `RefreshUseCaseExecutor` | `application/executor/refresh-use-case.executor.ts` | Si el resultado no es `UNAUTHORIZED` lo devuelve intacto; un segundo `UNAUTHORIZED` tras el reintento se devuelve sin otro refresh |

Cada archivo tiene su `.spec.ts` al lado (incluye dos operaciones concurrentes → un solo refresh). Los specs deciden por `error.type`: un `UNAUTHORIZED` con otro `code` (p. ej. `Session.Expired`) también refresca.

## No cableado

No hay tokens en `src/data/auth`, adapter de `SessionRefresherPort`, storage de credenciales, cierre de sesión ni uso del executor en ninguna operación. El adapter HTTP traduce el 401 a `AppError` con `toAppError` ([errors-and-results.md](errors-and-results.md)), sin perder `UNAUTHORIZED`.

## Pendiente de decisión

- Endpoint de refresh y contrato de tokens: el backend aún no los expone.
- Cableado: tokens `data/auth`, adapter, storage, cierre de sesión y dónde se envuelven las operaciones protegidas con el executor.
