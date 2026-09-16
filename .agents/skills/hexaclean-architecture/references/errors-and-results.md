# Errores y resultados

Vista frontend del [contrato de error unificado](../../backend-architecture/references/api-error-contract.md). Si hay contradicción, manda el contrato. Rutas relativas a `MarketjoyaFront/`.

## GoResult (`src/base/go-result.type.ts`, `src/base/go-result.ts`)

```ts
type GoOk<T> = readonly [data: T, error: null];
type GoErr<E> = readonly [data: null, error: E];
type GoResult<T, E> = GoOk<T> | GoErr<E>;
```

- Construye solo con `goOk(data)` / `goErr(error)`; ambos lanzan `TypeError` con `null`/`undefined`.
- Éxito sin dato: `goOk(true)` (patrón de `core/auth`).
- Narrowing: `const [data, error] = result; if (error !== null) { ... }` → después `data` es `T`. No uses `if (error || !data)`.
- Puertos, casos de uso y facades devuelven `GoResult<T, AppError>`.

## AppError, ErrorType y FieldError (`src/base/`)

```ts
type ErrorType =
  | 'VALIDATION' | 'UNAUTHORIZED' | 'FORBIDDEN' | 'NOT_FOUND'
  | 'CONFLICT' | 'FAILURE' | 'NETWORK' | 'UNKNOWN';

interface FieldError {
  readonly field: string; // ruta JSON camelCase del body del request: items[0].quantity
  readonly code: string; // Quantity.GreaterThanZero o Validation.<Regla>
  readonly description: string;
}

interface AppError {
  readonly type: ErrorType;
  readonly code: string; // <Ámbito>.<Motivo>: Sale.NotFound
  readonly description: string;
  readonly fieldErrors: readonly FieldError[]; // [] si no aplica
  readonly status?: number;
  readonly traceId?: string;
  readonly correlationId?: string;
  readonly retryAfterSeconds?: number; // Retry-After en 429/503
}
```

- `ErrorType` es lista cerrada (contrato §2): agregar un valor es cambio de contrato en las tres plataformas.
- `code` es API pública (§3): no se renombra ni se reutiliza.

## Traducción HTTP: el adapter es el único traductor

Mapper compartido `src/infrastructure/http/to-app-error.mapper.ts` (`toAppError(error: unknown, sentCorrelationId?: string): AppError`). Todo adapter HTTP devuelve `goErr(toAppError(error))` en su `catch`; `sentCorrelationId` solo hace falta en errores no HTTP, porque `correlationIdInterceptor` ya expone el id enviado en los headers del `HttpErrorResponse`. Ninguna otra capa lee `HttpErrorResponse` ni ProblemDetails, y ningún adapter parsea ProblemDetails por su cuenta.

**Manda el status** (contrato §6.1). `AppError.type` sale siempre del status HTTP; el `type` del ProblemDetails es la URI del RFC y nunca se lee como categoría.

| Status | `type` | `code` si el cuerpo falta, no es JSON o no trae `code` |
|---|---|---|
| 400 | `VALIDATION` | `Http.BadRequest` |
| 401 | `UNAUTHORIZED` | `Auth.Unauthorized` |
| 403 | `FORBIDDEN` | `Auth.Forbidden` |
| 404 | `NOT_FOUND` | `Http.NotFound` |
| 409 | `CONFLICT` | `Http.Conflict` |
| 5xx (incluidos 502/503/504) | `FAILURE` | `Server.Failure` |
| 408 | `NETWORK` | `Network.Timeout` |
| 429 (p. ej. rate limit de Traefik, cuerpo texto plano) | `NETWORK` | `Network.RateLimited` |
| 0 (sin respuesta) | `NETWORK` | `Network.Unavailable` |
| `TimeoutError` de rxjs | `NETWORK` | `Network.Timeout` |
| Otro status (3xx, 418, 422…) | `UNKNOWN` | `Unknown.Unexpected` (conserva el `code` del cuerpo si viene) |
| Error que no es `HttpErrorResponse` | `UNKNOWN` | `Unknown.Unexpected` |

- `code` = `code` del ProblemDetails si viene; si no, el reservado de la tabla.
- `description` = `detail` → `title` → `''`. En `<UseCase>.Validation` el `detail` es un resumen fijo: el detalle está en `fieldErrors`. En `FAILURE` el backend no expone la descripción interna.
- `fieldErrors` = `errors` del 400, campo a campo (`field`, `code`, `description`); `[]` en el resto o si el cuerpo no es interpretable.
- `field` es la ruta del body del request; si el formulario usa otra ruta, el adapter la traduce.
- El cliente genera y envía `X-Correlation-Id` en todo request (interceptor). `correlationId` = el del cuerpo → header de respuesta → id enviado; así existe también en `NETWORK`. `traceId` del cuerpo.
- `status` siempre que exista respuesta HTTP.
- Reintento de `NETWORK` (incluidos 408 y 429) y `FAILURE`: solo en operaciones idempotentes (query o command con `client_mutation_id`); en 429 respeta `Retry-After`.
- `retryAfterSeconds` = header `Retry-After` de 429/503 (segundos, o HTTP-date convertida a segundos desde ahora); ausente si no hay header válido.

## Consumo en casos de uso y UI

- Ramifica por `type` primero y por `code` después: `error.type === 'CONFLICT' && error.code === 'Concurrency.Conflict'`. Nunca por `description`, `detail` ni `title`.
- Use case propaga el `AppError` intacto o devuelve uno propio con `code` estable; nunca lo degrada.
- Facade no interpreta el error. Service decide estado, mensaje y feedback.
- Nunca conviertas un fallo distinguible en `UNKNOWN` ni en `FAILURE`.

## Mensaje al usuario (contrato §6.3)

| Caso | Mensaje |
|---|---|
| Existe texto local para `code` | Texto local |
| 4xx sin texto local | `description`; genérico por `type` si está vacía |
| `FAILURE`, `NETWORK`, `UNKNOWN` sin texto local | Genérico por `type` |

- En `FAILURE`, `NETWORK` y `UNKNOWN` muestra siempre `correlationId` como referencia de soporte.
- `fieldErrors` se pintan junto al control cuya ruta coincide con `field`; los que no casan van al resumen del formulario. El estado de error no se comunica solo con color (`../../design-system/SKILL.md`).

## UNAUTHORIZED

Operación protegida → refresh de sesión single-flight y un reintento; si el refresh falla o vuelve `UNAUTHORIZED`, cierre de sesión ([refresh-session.md](refresh-session.md)). No se muestra como error genérico. `FORBIDDEN` no refresca ni reintenta.

## Logs

Registra `type`, `code`, `status`, `correlationId` (y `traceId` si existe). Sin PII, payload, tokens ni cabeceras de autorización.

## Estado de implementación

Implementado (contrato §8):

- `ErrorType`, `FieldError` y `AppError` en `src/base/app-error.type.ts` (`@base/app-error.type`).
- `toAppError(error: unknown, sentCorrelationId?: string)` en `src/infrastructure/http/to-app-error.mapper.ts`.
- `correlationIdInterceptor`, `CORRELATION_ID_HEADER` y `createCorrelationId` en `src/infrastructure/http/correlation-id.interceptor.ts`, registrado con `withInterceptors` en `src/app.config.ts` ([data-and-di.md](data-and-di.md)).
- `RefreshUseCaseExecutor` ramifica por `type === 'UNAUTHORIZED'` ([refresh-session.md](refresh-session.md)).

Pendiente de código:

- Logs de cliente (contrato §6.6).
- Cuerpos `Blob`/`ArrayBuffer`: el mapper no los lee y aplica el código reservado del status.
- `fieldErrors` solo se leen en 400; en cualquier otro status quedan `[]`.
- Catálogo de textos por `code`: ver Pendiente de decisión.

## Pendiente de decisión

- Catálogo de textos locales por `code` (ubicación e i18n).
