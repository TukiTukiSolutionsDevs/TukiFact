# Checklist: verificación

Ejecutar en `MarketjoyaFront/`.

- [ ] `npm run verify` en verde (format:check, lint, typecheck, tokens:check, test, build).
- [ ] `npm run e2e` si se tocó un flujo crítico.
- [ ] Si cambió `eslint.config.js`: `npm run lint:architecture` y fixtures actualizados.
- [ ] Se informa cualquier comando no ejecutado y su motivo.
- [ ] Rutas con sesión ausente, cuando exista auth cableada.
- [ ] Permisos de lectura y escritura.
- [ ] Estados loading, vacío, error y éxito.
- [ ] Error: `fieldErrors` en su campo; `correlationId` visible en fallos `FAILURE`/`NETWORK`/`UNKNOWN`.
- [ ] Persistencia/storage solo si ya existe un adapter para ello.
