# Checklist: revisión arquitectónica Android

- [ ] `:architecture-tests` en verde (Domain puro, UseCase/ViewModel/Composable sin infraestructura, paquete = módulo, capas del slice, UseCase por paquete, sin literales visuales).
- [ ] DTO ≠ Entity ≠ Domain ≠ UiModel, con mapper explícito.
- [ ] Contrato HTTP = Command/Query existente bajo `/api/v1/...`, no inventado.
- [ ] UseCase retorna `AppResult<T>`; errores `AppError` estáticos por agregado (`<Aggregate>Errors`, `code` `<Ámbito>.<Motivo>`); sin `kotlin.Result` ni excepciones para reglas predecibles.
- [ ] Solo `HttpErrorMapper` traduce ProblemDetails → `AppError` (lee `code`, `errors`, `traceId`, `correlationId`; 403 → `Forbidden`); UI ramifica por `type` y luego `code`.
- [ ] Outbox: `Network`/`Failure` reintentan con el mismo `ClientMutationId`; `Unauthorized` refresh + un reintento, si falla queda pendiente; `Concurrency.Conflict` no se descarta; `Validation`/`Forbidden`/`NotFound`/otro `Conflict`/`Unknown` rechazan guardando el `AppError`.
- [ ] Sync con `LocalId` + `ClientMutationId` o declarado online-only.
- [ ] Visual solo tokens generados / `Mr*`.
- [ ] Fiscal/SUNAT no está en el APK; sin secretos ni firma en el repo.
- [ ] Sin `GlobalScope` ni UI en singleton (detekt).
- [ ] Dependencias nuevas en `gradle/libs.versions.toml`.
