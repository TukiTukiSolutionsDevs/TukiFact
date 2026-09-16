# E2E web (Playwright)

Pocos. No cada CRUD. Lista crítica alineada al backend: sesión y, si hay UI de dinero/stock, esos flujos.

## Configuración real (`MarketjoyaFront/playwright.config.ts`)

- `testDir: './e2e'`; un subdirectorio por flujo (`e2e/shell/app-shell.spec.ts`).
- `webServer`: `npx ng serve --port 4200`, `reuseExistingServer` fuera de CI, timeout 120 s.
- `baseURL` `http://localhost:4200`; `testIdAttribute: 'data-testid'`.
- Proyecto único `chromium`; `fullyParallel`; en CI `retries: 2` y `forbidOnly`; `trace: 'on-first-retry'`.
- Artefactos en `tmp/playwright/` (ignorado por git).
- Tipado con `tsconfig.e2e.json`; lint incluido en `npm run lint`.

Ejecutar: `npm run e2e` (requiere `npx playwright install chromium`). No forma parte de `npm run verify`.

## Cómo

- Locators: `getByTestId`. Prohibido `page.waitForTimeout`.
- `webServer` solo en la config, nunca en el test.
- API: mock controlado (`page.route`) **o** backend de test compartido, con datos únicos. No recrear DB por test (`backend-testing`).
- Preflight en CI: typecheck/build **antes** de Playwright.

## Escenarios mínimos

- [ ] Shell carga (`e2e/shell/app-shell.spec.ts`, existente).
- [ ] Sesión ausente → acceso bloqueado (cuando exista auth).
- [ ] 403 o botón oculto.
- [ ] Un happy path de negocio si existe pantalla.
- [ ] Un error de negocio visible (no pantalla blanca).

## Pendiente de decisión

- Login y reutilización de sesión (`storageState` / setup project): el backend no expone login.
- Mock de API vs backend de test compartido para los flujos de negocio.
