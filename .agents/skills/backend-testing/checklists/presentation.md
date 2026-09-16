# Checklist: Presentation (E2E)

- [ ] El flujo está en la lista crítica (dinero/stock/caja/auth) o se justifica por qué es E2E.
- [ ] `[Collection(ApiCollection.Name)]` + `ApiFixture` (una sola DB y contenedores por colección).
- [ ] Token real con `TestCaller.Unique()` y los permisos del endpoint; rutas con `ApiRoutes.V1`.
- [ ] Happy path 2xx y efecto observable.
- [ ] 401/403 cubiertos.
- [ ] Un fallo de negocio → ProblemDetails con status y `code` del estático (no 500), asertado con `ShouldBeProblemAsync` (incluye `traceId` y `correlationId`).
- [ ] Request inválido → 400 con `code` `<CasoDeUso>.Validation`, `detail` "La solicitud tiene errores de validación." y `errors` con `field` y `code` (contrato §7).
- [ ] Asserts por `code`; `detail` solo el texto fijo del 400; nunca `description`.
- [ ] `X-Correlation-Id` preservado o generado si el flujo lo requiere.
- [ ] SUNAT/IA/GPS no pegan al proveedor real (mecanismo en `ApiFactory` pendiente de decisión).
- [ ] No re-aserta invariantes que ya cubre Domain ni el contrato genérico del host.
