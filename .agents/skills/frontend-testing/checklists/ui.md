# Checklist: UI

- [ ] Service/store con token fake; store solo en éxito.
- [ ] Estado `error` conserva `type` + `code` del `AppError`.
- [ ] `correlationId` visible en fallos `FAILURE`/`NETWORK`/`UNKNOWN`.
- [ ] `fieldErrors` de `VALIDATION` llegan a su campo.
- [ ] Mensaje no depende de `description` para decidir.
- [ ] Loading / empty / error cubiertos si la Screen tiene lógica.
- [ ] Guard: anónimo, lectura, sin permiso de mutación.
- [ ] `data-testid` en acciones si habrá E2E.
