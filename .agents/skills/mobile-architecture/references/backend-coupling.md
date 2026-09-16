# Acoplamiento al backend Market Real

El móvil no redefine el modelo de negocio. Consume el host del monolito modular. Fuente: `../backend-architecture/SKILL.md`. Contrato de error: `../../backend-architecture/references/api-error-contract.md`.

## Contrato HTTP

| Tema | Backend (existe) | Móvil |
|---|---|---|
| Rutas | `/api/v1/...` | Consume solo rutas públicas del host |
| Contrato | OpenAPI 3.1 comprometido en `MarketjoyaBackend/openapi/marketjoya-api-v1.json`; `operationId` = `<Module>_<UseCase>` | Generación de cliente: Pendiente de decisión |
| Enums | Serializados como números | Mapeo a enums Kotlin: Pendiente de decisión |
| Errores | RFC 9457 ProblemDetails con extensiones `code`, `traceId`, `correlationId`, `errors` (400) | `HttpErrorMapper` (`:core:network`) → `AppError` |
| Traza | Lee `X-Correlation-Id` | `CorrelationIdInterceptor` genera y envía `X-Correlation-Id` en todo request; el mapper lo guarda en `AppError.correlationId` |
| Auth | JWT bearer con claims `sub`, `company`, `warehouse`, `cash_register`, `permissions` | Base URL, endpoints de auth y almacenamiento de tokens: Pendiente de decisión |
| Concurrencia | `sync_version` en agregados; escritura con versión vieja → 409 `Concurrency.Conflict` | Cómo el móvil recibe y reenvía la versión: Pendiente de decisión |

## Traducción de errores

Solo el adapter de red (`HttpErrorMapper` en `:core:network`) traduce ProblemDetails → `AppError`. Ni el UseCase, ni el ViewModel, ni la Screen leen status HTTP ni JSON de error.

| Respuesta | `ErrorType` | `code` si falta en el cuerpo |
|---|---|---|
| 400 | `Validation` | `Http.BadRequest` (el backend normalmente envía `<UseCase>.Validation`) |
| 401 | `Unauthorized` | `Auth.Unauthorized` |
| 403 | `Forbidden` (nunca `Failure`) | `Auth.Forbidden` |
| 404 | `NotFound` | `Http.NotFound` |
| 409 | `Conflict` (`BusinessRule.<Regla>`, `Concurrency.Conflict`, duplicados) | `Http.Conflict` (en outbox es "otro `Conflict`": rechazada) |
| 408 | `Network` | `Network.Timeout` |
| 429 (rate limit de Traefik, cuerpo texto plano; los POS comparten la IP de la tienda) | `Network` | `Network.RateLimited` + `retryAfterSeconds` desde `Retry-After` |
| 5xx (incluidos 502/503/504 con HTML de proxy) | `Failure` | `Server.Failure` (503 con `Retry-After` → `retryAfterSeconds`) |
| `IOException` sin respuesta | `Network` | `Network.Unavailable` |
| `SocketTimeoutException` / timeout | `Network` | `Network.Timeout` |
| Status fuera de la tabla (3xx, 418, 422, …) | `Unknown` | `code` del cuerpo si viene; si no `Unknown.Unexpected` (con `status`) |

Reglas del mapper:

- Lee `code`, `errors` (→ `fieldErrors`), `traceId` y `correlationId`; `status` = status HTTP.
- `code` presente en el cuerpo gana siempre sobre el reservado.
- `description` = `detail` → `title` → cadena vacía. Ninguna lógica la lee; en `Failure` no expone detalle interno.
- Manda el status (§6.1): cuerpo ausente, no JSON o sin `code` con status de la tabla → ese `type` + su código reservado. Status fuera de la tabla → siempre `Unknown`, conservando el `code` del cuerpo si existe.
- `retryAfterSeconds` (opcional) sale de `Retry-After` en 429/503: segundos tal cual; fecha HTTP convertida a segundos. El mapper lo fija; el outbox lo lee.
- `correlationId` = el generado por `CorrelationIdInterceptor` para ese request, así existe también en `Network` (sin respuesta HTTP).
- No conviertas todo a un error genérico de red: `Network` solo sin respuesta HTTP, o con 408/429.
- Log: `type`, `code`, `status`, `correlationId`. Sin PII ni payload.

Implementado (`:core:network`): `HttpErrorMapper` es una clase con `Clock` inyectable; `map(response, body)` traduce respuestas HTTP y `map(request, IOException)` los fallos sin respuesta. `error/ProblemDetails.kt` lee el cuerpo y `CorrelatedIOException` (lanzada por `CorrelationIdInterceptor`) conserva el id enviado. Pruebas: `HttpErrorMapperTest`, `NetworkSuiteTest`. Pendiente de código: log de cliente (contrato §6.6).

## Mapa de intenciones

| En el dispositivo | En el API |
|---|---|
| `CompleteSaleUseCase` | Command del módulo → `POST /api/v1/...` |
| Puerto de lectura | Query → `GET /api/v1/...` |
| `AppError` | `Error` + `ToHttpResult()` |
| `LocalId` + `ClientMutationId` | Idempotencia (misma mutación ×2 = un efecto) |
| Outbox local `PendingSyncOperation` | Outbox de integración del servidor (independiente) |

## Qué el móvil no hace

- No publica al broker ni escribe el outbox del servidor.
- No combina módulos del API ni sus schemas: un solo host, endpoints públicos.
- No genera documentos fiscales ni lleva claves SUNAT.
- No expone agregados como DTO; el Response del API es plano.

## Offline

Flujo, reintentos y decisiones abiertas: `references/data-and-offline.md`. El backend es la autoridad ante un conflicto.

## Auth

TLS obligatorio. El backend autoriza; la UI solo oculta acciones. `Unauthorized`: refresh de sesión single-flight una vez; si falla, cierre de sesión. Endpoints y protocolo de refresh: Pendiente de decisión.
