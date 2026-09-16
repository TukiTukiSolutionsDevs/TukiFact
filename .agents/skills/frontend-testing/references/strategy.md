# Estrategia frontend

Cobertura por riesgo, no por %. Lo que rompe plata, sesión o permiso va primero.

## Pirámide

```text
        ╱ E2E ╲              ← flujos críticos (Playwright, browser real)
       ╱       ╲
      ╱ Integración ╲        ← adapters HTTP + wiring data
     ╱               ╲
    ╱    Unitarias     ╲    ← core (use cases) + service/store
   ╱                    ║   + tooling (lint de fronteras, tokens, Tailwind)
```

Core unitario es la base: las reglas viven ahí, no en el template. Integración del adapter demuestra el mapeo ProblemDetails → `AppError` (`type` + `code`; `UNAUTHORIZED` no se vuelve error genérico). E2E no sustituye eso.

## Dónde viven (`MarketjoyaFront/`)

```text
src/
├── base/go-result.spec.ts                                      ← junto al archivo
├── core/<feature>/application/use-case/<op>.use-case.spec.ts
├── infrastructure/http/to-app-error.mapper.spec.ts               ← mapper compartido
├── infrastructure/<feature>/adapter/out/<capability>.adapter.spec.ts
└── ui/<feature>/<feature>.service.spec.ts
tools/<tool>/<name>.spec.mts                                    ← tooling (Node)
e2e/<flujo>/<critical-flow>.spec.ts                             ← Playwright
```

Ejemplos reales: `src/core/auth/application/**/*.spec.ts`, `e2e/shell/app-shell.spec.ts`.

## Stack

| Propósito | Herramienta | Configuración |
|---|---|---|
| Unit (`src/**/*.spec.ts`) | Vitest 4 vía `@angular/build:unit-test` (jsdom instalado) | `angular.json` → `test`; `tsconfig.spec.json` con `vitest/globals` (`describe`/`it`/`expect` sin import) |
| Tooling (`tools/**/*.spec.mts`) | Vitest 4, entorno `node`, timeout 30 s | `vitest.tooling.config.mts`; imports explícitos desde `vitest` |
| DI / HTTP de prueba | TestBed + `provideHttpClient` + `provideHttpClientTesting` | Adapter y guards |
| E2E | Playwright 1.63, proyecto `chromium` | `playwright.config.ts`, carpeta `e2e/` |
| Locators | `data-testid` | `testIdAttribute: 'data-testid'` |
| Fakes | Clases in-memory que implementan el puerto | Preferir fake a `vi.fn()` vacío |

## Scripts

`npm test` = `test:unit` (`ng test --watch=false`) + `test:tooling`. `npm run test:watch`, `npm run lint:architecture`, `npm run e2e`. `npm run verify` incluye `test` pero no `e2e`.

## Tooling existente

- `tools/architecture-lint/architecture-lint.spec.mts`: fronteras de capas.
- `tools/tokens/tokens.spec.mts`, `tools/tokens/run-tokens.spec.mts`: generador de CSS de tokens.
- `tools/tailwind/tailwind-layout-only.spec.mts`: Tailwind solo layout.

## Obligatorio antes de producción (web)

- Auth: login, 401 → refresh una vez, 403 visible (login/refresh backend: Pendiente de decisión).
- Mutación de dinero/stock si hay pantalla web (misma lista crítica que backend).
- `type` + `code` del `AppError` conservados hasta el service (`UNAUTHORIZED`, `NOT_FOUND`, `CONFLICT`); `correlationId` visible en fallos.
- Guard de ruta + botón oculto alineados.

## No se prueba

- Angular, change detection del framework, `Mr*` interno del DS (salvo contrato de variante nueva).
- Mappers 1:1 de campos.
- Cada Screen que solo llama al service.

## Rendimiento

Un `webServer` por suite Playwright. Prohibido resetear todo el `TestBed` de la app en cada `it` si basta un fake del puerto.
