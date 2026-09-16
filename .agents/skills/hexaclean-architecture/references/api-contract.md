# Contrato del API

Fuente: `MarketjoyaBackend/openapi/marketjoya-api-v1.json` (OpenAPI 3.1, versionado en el repo; el backend falla sus tests si el archivo deriva). Guía del backend: `MarketjoyaBackend/docs/openapi.md`.

## Reglas

- Rutas: prefijo `/api/v{version}`; hoy `/api/v1/...`. Los adapters usan rutas relativas `/api/v1/<resource>`.
- `operationId` = `<Module>_<UseCase>`. Cita el `operationId` al crear o revisar un adapter.
- Paths, verbos, DTOs y enums salen del documento. Si la operación no está, bloquea y pide el cambio backend; no inventes endpoints ni campos.
- Enums: el backend los serializa como números.
- Seguridad: esquema `Bearer`; los permisos requeridos aparecen como scopes de cada operación.
- Errores: `application/problem+json` con `code`, `traceId`, `correlationId` y `errors` (solo 400). El `type` del cable es la URI del RFC, no la categoría: el adapter deriva la categoría del status. Si falta `code` (p. ej. 401/403/404 del framework, 5xx no controlado), el adapter aplica el reservado: `Http.BadRequest`, `Auth.Unauthorized`, `Auth.Forbidden`, `Http.NotFound`, `Http.Conflict` o `Server.Failure`; 408 → `Network.Timeout` y 429 → `Network.RateLimited` (respetar `Retry-After`) ([errors-and-results.md](errors-and-results.md), [contrato canónico](../../backend-architecture/references/api-error-contract.md)).
- DTOs (escritos a mano o generados) viven solo en `src/infrastructure/` y se mapean a tipos de `src/core/`.
- Correlación: el backend acepta `X-Correlation-Id` (conserva un id bien formado o genera uno) y lo devuelve en la cabecera de respuesta y en `correlationId`.

## Estado actual

- `paths` del documento está vacío: no hay endpoints publicados todavía.
- `src/environment/` no define base URL.
- `correlationIdInterceptor` (`src/infrastructure/http/correlation-id.interceptor.ts`) genera y envía `X-Correlation-Id` en todo request; registrado en `src/app.config.ts` ([data-and-di.md](data-and-di.md)).

## Pendiente de decisión

- Herramienta de generación de cliente/DTOs para el frontend. La guía del backend menciona opciones con rutas `src/app/...` que ya no existen en `MarketjoyaFront`; no las adoptes sin decisión.
- Enums numéricos vs string en el contrato (backend).
- Base URL por entorno en `src/environment/`.
