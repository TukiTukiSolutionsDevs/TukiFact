# Capa: infrastructure (adapters)

El adapter traduce HTTP/storage → dominio + `AppError`. Aquí se cazan bugs de mapeo. Contrato: [api-error-contract.md](../../backend-architecture/references/api-error-contract.md) (§3, §4, §7); vista frontend: [errors-and-results.md](../../hexaclean-architecture/references/errors-and-results.md).

## HTTP

```ts
TestBed.configureTestingModule({
  providers: [provideHttpClient(), provideHttpClientTesting(), <Feature>WriterAdapter],
});
```

Secuencia: **llamar → `expectOne` → `flush`/`error` → await**. Nunca `await` antes de `flush`.

`afterEach(() => http.verify())`: requests no esperados fallan.

## Casos mínimos del mapper (contrato §7)

Van en `src/infrastructure/http/to-app-error.mapper.spec.ts` como `it.each` con fixtures JSON de ProblemDetails:

Manda el status: `AppError.type` sale del status; el `type` del ProblemDetails es la URI del RFC y no cuenta.

| Caso | Respuesta simulada | `AppError` esperado |
|---|---|---|
| ProblemDetails con `code` | 409 `{ type: '<uri RFC>', code: 'Concurrency.Conflict', detail, traceId, correlationId }` | `CONFLICT` / `Concurrency.Conflict`, `description` = `detail`, `status`, `traceId`, `correlationId` |
| 400 con `errors` | 400 `{ code: 'CreateSale.Validation', errors: [{ field: 'items[0].quantity', code, description }] }` | `VALIDATION` / `CreateSale.Validation`, `fieldErrors` 1:1 |
| 400 sin `code` | 400 sin cuerpo (JSON mal formado rechazado por el framework) | `VALIDATION` / `Http.BadRequest`, `fieldErrors: []` |
| 401 sin `code` | 401 sin cuerpo | `UNAUTHORIZED` / `Auth.Unauthorized` |
| 403 sin `code` | 403 sin cuerpo | `FORBIDDEN` / `Auth.Forbidden` |
| 404 sin `code` | 404 ProblemDetails sin `code` | `NOT_FOUND` / `Http.NotFound` |
| 409 sin `code` | 409 sin cuerpo | `CONFLICT` / `Http.Conflict` |
| 408 | 408 sin cuerpo | `NETWORK` / `Network.Timeout` |
| 429 | 429 texto plano (Traefik) + header `Retry-After: 30` | `NETWORK` / `Network.RateLimited`, `status: 429`, `retryAfterSeconds: 30` |
| 503 con HTTP-date | 503 + `Retry-After: <fecha>` (reloj fijo con `vi.useFakeTimers`) | `FAILURE` / `Server.Failure`, `retryAfterSeconds` calculado |
| 5xx sin `code` | 500 sin cuerpo; 502/503/504 con HTML de proxy | `FAILURE` / `Server.Failure` |
| Status fuera de §2 con `code` | 422 `{ code: 'Sale.Unprocessable' }` | `UNKNOWN` / `Sale.Unprocessable`, `status: 422` |
| `correlationId` en `NETWORK` | red + id enviado por el interceptor | `NETWORK` con `correlationId` = id enviado |
| Status fuera de §2, cuerpo no JSON | 418 con `'<html>…</html>'` | `UNKNOWN` / `Unknown.Unexpected`, `status: 418` |
| Error de red | `error(new ProgressEvent('error'))` | `NETWORK` / `Network.Unavailable` |
| Error no HTTP | `new Error()` | `UNKNOWN` / `Unknown.Unexpected` |
| `type` URI engañoso | 404 con `type` URI de otro status | `NOT_FOUND` (el status manda) |
| `correlationId` en header | 404 sin cuerpo + header `X-Correlation-Id` | `correlationId` del header |

- ProblemDetails: `flush(body, { status, statusText, headers: { 'Content-Type': 'application/problem+json' } })`.
- Sin cuerpo: `flush(null, { status, statusText: 'Error' })`.
- Red: `expectOne(url).error(new ProgressEvent('error'))`.
- Asserta `type` y `code` siempre; nunca `description` como criterio de clasificación.

Un adapter no repite la tabla: prueba mapeo de éxito y, con al menos un caso, que devuelve el `AppError` del mapper (`type` + `code`) para los errores que su operación puede producir.

Referencias en código: `src/infrastructure/http/to-app-error.mapper.spec.ts` (mapper) y `src/infrastructure/http/correlation-id.interceptor.spec.ts` (header `X-Correlation-Id`).

Prohibido mockear `HttpClient` con `vi.fn()`. El transport de prueba es `HttpTestingController`.

## Storage

Fake in-memory del storage. No `localStorage` real compartido entre `it`.

## Refresh

Single-flight y reintento único se prueban en core: `src/core/auth/application/use-case/refresh-session.use-case.spec.ts` y `src/core/auth/application/executor/refresh-use-case.executor.spec.ts` (dos operaciones concurrentes → un solo refresh). El adapter de refresh no existe (Pendiente de decisión). Ver [hexaclean-architecture: refresh-session.md](../../hexaclean-architecture/references/refresh-session.md).
