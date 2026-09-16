# Checklist: Infrastructure

- [ ] Room/MockWebServer de clase (`@BeforeClass`/`@AfterClass`), no por `@Test`.
- [ ] Assert por `ClientMutationId` / `LocalId`, no conteo global.
- [ ] Adapter con fake existente: extiende el contrato de `:core:testing`.
- [ ] HTTP: fixtures JSON reales de ProblemDetails; `X-Correlation-Id` presente.
- [ ] Mapper (contrato §7): con y sin `code` (reservados `Http.BadRequest`, `Http.Conflict`, `Auth.Unauthorized`, `Auth.Forbidden`, `Http.NotFound`, `Server.Failure`); 400 con `errors` → `fieldErrors`; 403 → `Forbidden`; 502 HTML → `Failure`/`Server.Failure`; status fuera de §2 → `Unknown` (422 con `code` lo conserva; sin `code` → `Unknown.Unexpected`); `description` = `detail` → `title` → ""; sin respuesta → `Network.Unavailable`; timeout y 408 → `Network.Timeout`; 429 texto plano → `Network.RateLimited` con `retryAfterSeconds` (segundos o fecha HTTP).
- [ ] Aserta `type`, `code`, `status`, `traceId`, `correlationId`; error `Network` conserva el `correlationId` generado.
- [ ] Hardware: fake del puerto o parser unitario cuando exista el puerto; nunca USB en CI.
