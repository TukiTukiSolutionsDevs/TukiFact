# Checklist: adapter

- [ ] `provideHttpClient` + `provideHttpClientTesting`.
- [ ] `expectOne` → `flush`/`error` → await. `verify()` en `afterEach`.
- [ ] 2xx mapea a dominio.
- [ ] Adapter único traductor: devuelve el `AppError` de `toAppError`, sin parseo propio.
- [ ] Mapper compartido cubre contrato §7 (manda el status): ProblemDetails con `code`; 400/401/403/404/409/5xx sin `code` → `Http.BadRequest`/`Auth.Unauthorized`/`Auth.Forbidden`/`Http.NotFound`/`Http.Conflict`/`Server.Failure`; 408 → `NETWORK`/`Network.Timeout`; 429 (texto plano, `Retry-After: 30`) → `NETWORK`/`Network.RateLimited` con `retryAfterSeconds: 30`; 400 con `errors` → `fieldErrors`; 502 con HTML → `FAILURE`/`Server.Failure`; status fuera de §2 → `UNKNOWN` (422 conserva `code`; 418 no JSON → `Unknown.Unexpected`); `NETWORK` conserva el `correlationId` enviado; red → `NETWORK`.
- [ ] El `type` URI del ProblemDetails no influye en `AppError.type`.
- [ ] Red (`ProgressEvent`) → `NETWORK`, nunca éxito ni `UNKNOWN`.
- [ ] Se asserta `type` + `code` (y `status`, `correlationId` cuando aplica); nunca `description` como criterio.
- [ ] URL del OpenAPI (`/api/v1/...`).
- [ ] Refresh: single-flight probado en core, no en el adapter.
