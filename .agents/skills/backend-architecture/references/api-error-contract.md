# Contrato de error unificado (canónico)

Fuente única del modelo de error entre backend, frontend y mobile. Las skills de cada plataforma solo describen **cómo** lo mapean; si hay contradicción, manda este archivo.

## 1. Principios

- Un error se identifica por **`type`** (categoría cerrada) y **`code`** (motivo estable). Nunca por el texto.
- Errores previsibles viajan como valor (`Result` / `GoResult` / `AppResult`), no como excepción, en las tres plataformas.
- El `code` se conserva de extremo a extremo: dominio/handler → HTTP → adapter → caso de uso → UI.
- `description` es para humanos; ningún cliente decide lógica leyendo `description` o `detail`.
- Toda respuesta de error lleva `traceId` y `correlationId` para soporte.

## 2. Categorías (`type`)

Lista cerrada. Agregar una categoría es un cambio de contrato en las tres plataformas.

| `type` | HTTP | Origen | Reintentable |
|---|---|---|---|
| `VALIDATION` | 400 | Backend (validador) | No |
| `UNAUTHORIZED` | 401 | Backend (autenticación) | Tras refresh de sesión, una vez |
| `FORBIDDEN` | 403 | Backend (permiso) | No |
| `NOT_FOUND` | 404 | Backend | No |
| `CONFLICT` | 409 | Backend (regla de negocio, concurrencia, duplicado) | Solo `Concurrency.Conflict` tras recargar |
| `FAILURE` | 5xx | Backend (inesperado) | Sí, solo si la operación es idempotente |
| `NETWORK` | — / 408 / 429 | Cliente (sin respuesta HTTP, timeout, offline) o borde (Traefik rate limit, timeout) | Sí, solo si la operación es idempotente; en 429 respetar `Retry-After` |
| `UNKNOWN` | otro | Cliente (respuesta no clasificable) | No |

Idempotente = query, o command con `client_mutation_id`.

Casing por plataforma: backend `ErrorType` en PascalCase (`Validation`, `NotFound`, `Conflict`, `Failure`; `Unauthorized`/`Forbidden` los emite el framework); frontend y mobile usan los 8 valores (TS: string en MAYÚSCULAS; Kotlin: enum PascalCase).

## 3. Código (`code`)

- Formato `<Ámbito>.<Motivo>` en PascalCase, segmentos separados por punto: `Sale.NotFound`, `CashRegister.AlreadyOpen`.
- Es API pública: **no se renombra ni se reutiliza** con otro significado. Deprecar = dejar de emitir.
- Backend los declara estáticos por agregado (`<Agregado>Errors`), nunca inline.

Códigos reservados:

| `code` | `type` | Quién lo produce |
|---|---|---|
| `<UseCase>.Validation` | VALIDATION | Backend, `ValidationBehavior` |
| `BusinessRule.<Regla>` | CONFLICT | Backend, `BusinessRuleValidationException` |
| `Concurrency.Conflict` | CONFLICT | Backend, `sync_version` desactualizado |
| `Auth.Unauthorized` | UNAUTHORIZED | Cliente, si el 401 llega sin `code` |
| `Auth.Forbidden` | FORBIDDEN | Cliente, si el 403 llega sin `code` |
| `Http.BadRequest` | VALIDATION | Cliente, si el 400 llega sin `code` (p. ej. JSON mal formado rechazado por el framework) |
| `Http.NotFound` | NOT_FOUND | Cliente, si el 404 llega sin `code` (ruta o versión inexistente) |
| `Http.Conflict` | CONFLICT | Cliente, si el 409 llega sin `code` |
| `Server.Failure` | FAILURE | Cliente, si el 5xx llega sin `code` |
| `Network.Unavailable` / `Network.Timeout` | NETWORK | Cliente (sin respuesta, timeout local o 408) |
| `Network.RateLimited` | NETWORK | Cliente, ante 429 |
| `Unknown.Unexpected` | UNKNOWN | Cliente |

## 4. Forma en el cable (backend → clientes)

`application/problem+json` (RFC 9457). Campos estándar `type`, `title`, `status`, `detail`; extensiones:

> El `type` del cable es la **URI del RFC**, no la categoría. La categoría de §2 **no viaja**: el cliente la deriva del `status`. En los modelos de cliente el campo `AppError.type` es la categoría.

| Campo | Obligatorio | Nota |
|---|---|---|
| `code` | Sí en errores de negocio y validación. Puede faltar en respuestas del framework o de proxies (400 por JSON mal formado, 401, 403, 404, 409) y en 5xx por excepción no controlada | El cliente aplica el código reservado de §3 |
| `traceId` | Sí | |
| `correlationId` | Sí | Mismo valor que el header `X-Correlation-Id` |
| `errors` | Solo en 400 | Lista de `FieldError` |

`FieldError`: `{ "field": "items[0].quantity", "code": "Quantity.GreaterThanZero", "description": "..." }`.

- `field`: ruta JSON en camelCase del **body del request HTTP**. Los validadores corren sobre el Command, así que el Command usa los mismos nombres de propiedad que el Request.
- `code`: formato de §3. Cada regla de FluentValidation declara `.WithErrorCode("<Campo>.<Regla>")`. Si una regla no lo declara, el backend emite `Validation.<Regla>` (nombre del validador sin el sufijo `Validator`, p. ej. `Validation.NotEmpty`). Una falla sin código (p. ej. `AddFailure` dentro de `Custom`) se emite como `Validation.Custom`.
- `detail` del error `<UseCase>.Validation`: texto fijo "La solicitud tiene errores de validación."; el detalle está en `errors`.
- Todo 400 de tipo `VALIDATION` emitido por el backend incluye `errors`, aunque sea lista vacía (p. ej. `Error.Validation` devuelto directo por un handler), para que la forma sea estable.

En `FAILURE` el `detail` no expone la descripción interna.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "La solicitud tiene errores de validación.",
  "code": "CreateSale.Validation",
  "traceId": "00-4bf9…-01",
  "correlationId": "pos-7f3a",
  "errors": [
    { "field": "items[0].quantity", "code": "Quantity.GreaterThanZero", "description": "La cantidad debe ser mayor a cero." }
  ]
}
```

## 5. Modelo en cada plataforma

| Concepto | Backend | Frontend | Mobile |
|---|---|---|---|
| Resultado | `Result` / `Result<T>` | `GoResult<T, AppError>` | `AppResult<T>` (`Success` / `Failure(AppError)`) |
| Error | `Error(Code, Description, Type)` + `FieldErrors` en validación | `AppError` | `AppError` |
| Categoría | `ErrorType` | `ErrorType` | `ErrorType` |
| Error de campo | `FieldError(Field, Code, Description)` | `FieldError` | `FieldError` |

`AppError` (clientes) = `type`, `code`, `description`, `fieldErrors` (vacío si no aplica), y metadatos opcionales `status`, `traceId`, `correlationId`, `retryAfterSeconds` (del header `Retry-After` en 429/503; si viene como fecha HTTP se convierte a segundos).

Mobile: `AppResult` evita `kotlin.Result`, que exige `Throwable`.

## 6. Reglas de consumo en clientes

1. El adapter HTTP es el único que traduce ProblemDetails → `AppError`. **Manda el status**: si el cuerpo falta, no es JSON (p. ej. HTML de un proxy) o no trae `code`, el `type` sale del status (§2) y el `code` del reservado de §3 (400 → `Http.BadRequest`, 401 → `Auth.Unauthorized`, 403 → `Auth.Forbidden`, 404 → `Http.NotFound`, 409 → `Http.Conflict`, 408 → `NETWORK`/`Network.Timeout`, 429 → `NETWORK`/`Network.RateLimited`, 5xx incluidos 502/503/504 → `Server.Failure`). Status fuera de §2 (p. ej. 3xx, 418, 422): `UNKNOWN`; conserva el `code` del cuerpo si viene, si no `Unknown.Unexpected`. `description` = `detail` → `title` → cadena vacía.
2. Casos de uso y UI ramifican por `type` primero y por `code` después.
3. Mensaje al usuario: texto local por `code` si existe; si no, `description` para 4xx; genérico para `FAILURE`, `NETWORK` y `UNKNOWN`, mostrando `correlationId` como referencia de soporte. Cada cliente **genera y envía** `X-Correlation-Id` en todo request y lo guarda en el `AppError`, así existe también en `NETWORK`.
4. `errors` de validación se pintan en el campo correspondiente; nunca solo con color (design-system).
5. `UNAUTHORIZED`: refresh de sesión single-flight una vez; si falla, cierre de sesión.
6. Logs de cliente: `type`, `code`, `status`, `correlationId`. Sin PII ni payload.
7. Outbox (mobile), en este orden:
   - `NETWORK` y `FAILURE`: reintento con el mismo `client_mutation_id`.
   - `UNAUTHORIZED`: refresh de sesión y un reintento; si el refresh falla, la operación **queda pendiente** (nunca rechazada) hasta que se restaure la sesión.
   - `CONFLICT` con `Concurrency.Conflict`: flujo de conflicto del protocolo de sync (pendiente de decisión); no se descarta.
   - `VALIDATION`, `FORBIDDEN`, `NOT_FOUND`, otros `CONFLICT` y `UNKNOWN`: operación rechazada guardando el `AppError`.

## 7. Pruebas mínimas por plataforma

- Backend (E2E): cada `ErrorType` de `Result` produce su status, `code`, `traceId`, `correlationId`; 400 incluye `errors` con `field` y `code`; 401/403 del framework traen `traceId` y `correlationId` (sin `code`).
- Frontend y mobile (adapter): tabla de §2/§3 con fixtures JSON de ProblemDetails (con y sin `code`, con `errors`, cuerpo no JSON, 408, 429 con `Retry-After`, sin respuesta).

## 8. Estado de implementación

Las tres plataformas implementan el contrato (verificado con sus suites). Pendiente de código, no de contrato:

| Plataforma | Implementado | Pendiente |
|---|---|---|
| Backend | `FieldError`, `Error.FieldErrors`, `Error.Validation(code, description, fieldErrors)`; `ValidationBehavior` con `<UseCase>.Validation`, texto fijo y fallback `Validation.<Regla>`/`Validation.Custom`; 400 con `errors`; schema OpenAPI con `errors` | Ninguno del contrato |
| Frontend | `ErrorType`, `AppError`, `FieldError` en `@base/app-error.type`; `toAppError(error, sentCorrelationId?)` en `src/infrastructure/http/to-app-error.mapper.ts`; `correlationIdInterceptor` | Logs de cliente (§6.6); catálogo de textos por `code`; cuerpos Blob/ArrayBuffer usan códigos reservados |
| Mobile | `ErrorType` (8), `AppError`, `FieldError`, `AppResult`; `HttpErrorMapper` + `CorrelatedIOException`; `PendingSyncOperation` con `Rejected` + `rejection` (Room v2); `SyncAttemptPolicy`; regla Konsist `KotlinResultReturn` | Logs de cliente (§6.6); worker de sync y refresh de sesión (dependen de decisiones abiertas) |
