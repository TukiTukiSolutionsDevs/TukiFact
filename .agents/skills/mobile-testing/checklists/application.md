# Checklist: UseCase / ViewModel

- [ ] Fakes de puertos, no DAO.
- [ ] `runTest` + (VM) `MainDispatcherRule` / Turbine.
- [ ] Outbox + `clientMutationId` en éxito; nada en fallo.
- [ ] Fallo asertado como `AppResult.Failure` con `type` + `code`; sin `kotlin.Result` ni excepciones.
- [ ] Sync (§6.7): `Network`/`Failure` siguen `Pending` con el mismo `ClientMutationId`; `Unauthorized` con refresh fallido sigue `Pending`; `Concurrency.Conflict` → flujo de conflicto, no descartada; `Validation`/`Forbidden`/`NotFound`/otro `Conflict`/`Unknown` rechazan guardando el `AppError`.
- [ ] Idempotencia si es sync.
- [ ] Sin `sleep` / `GlobalScope`.
