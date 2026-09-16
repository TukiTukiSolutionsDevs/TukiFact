# Testing y verificación

Contrato runtime de pruebas: `../../frontend-testing/SKILL.md` (core unitario, adapter HTTP, service/store, Playwright).

## Scripts (`MarketjoyaFront/package.json`)

| Script | Hace |
|---|---|
| `npm run verify` | `format:check` + `lint` + `typecheck` + `tokens:check` + `test` + `build` |
| `npm test` | `test:unit` (`ng test --watch=false`) + `test:tooling` (Vitest de `tools/`) |
| `npm run test:watch` | `ng test` en modo watch |
| `npm run lint` | ESLint, incluye fronteras de capas |
| `npm run lint:architecture` | Pruebas negativas de fronteras |
| `npm run typecheck` | `tsc` de app, specs, tooling y e2e |
| `npm run tokens` / `tokens:check` | Genera / valida `src/styles/generated/` |
| `npm run e2e` | Playwright (arranca `ng serve`) |
| `npm run format` / `format:check` | Prettier |
| `npm run build` | Build de producción |

Antes de terminar: `npm run verify`. Si tocaste un flujo crítico, además `npm run e2e` (requiere `npx playwright install chromium`).

## Prioridad

1. Use cases en `core`, sin Angular.
2. Adapters: `HttpTestingController`, `type` y `code` del `AppError` conservados.
3. Services/stores: loading, error, store solo en éxito.
4. Guards: sesión y permisos (cuando existan).
5. E2E de flujos críticos (`data-testid`), no de cada pantalla.

No declares la suite verde sin haber ejecutado los scripts; reporta cualquier comando omitido y su motivo.
