# Reglas de dependencia

La matriz se aplica con `MarketjoyaFront/eslint.config.js` (eslint-plugin-boundaries 7, `default: 'disallow'`). Este archivo la describe; si divergen, manda la configuración y se corrige esta reference.

## Matriz (`boundaries/dependencies`)

| Elemento | Patrón | Puede importar |
|---|---|---|
| `app` | `src` (`main.ts`, `app.config.ts`) | todas las capas |
| `base` | `src/base/**` | `base` |
| `core` | `src/core/**` | `core`, `base` |
| `data-token-in` | `src/data/*/token/in/**` | `core`, `base` |
| `data-token-out` | `src/data/*/token/out/**` | `core`, `base` |
| `data` | `src/data/**` | `data`, `data-token-in`, `data-token-out`, `core`, `infrastructure`, `base` |
| `environment` | `src/environment/**` | `environment` |
| `infrastructure` | `src/infrastructure/**` | `infrastructure`, `core`, `base`, `environment`, `data-token-in`, `data-token-out` |
| `ui` | `src/ui/**` | `ui`, `core`, `data-token-in`, `base` |

Cuenta `import`, `export` y `import()` dinámico. `boundaries/no-unknown-files`: todo `.ts` de `src/` debe pertenecer a un elemento.

## Restricciones adicionales

| Capa | `no-restricted-imports` | `no-restricted-globals` |
|---|---|---|
| `base`, `core` | `@angular/*` | `window`, `document`, `location`, `history`, `navigator`, `localStorage`, `sessionStorage`, `indexedDB`, `fetch`, `XMLHttpRequest` |
| `data` | `@angular/common/http` | — |
| `infrastructure` | `@angular/router` | — |
| `ui` | `@angular/common/http`, `@infrastructure/**`, `**/infrastructure/**`, `**/token/out/**` | `window`, `localStorage`, `sessionStorage`, `indexedDB`, `fetch`, `XMLHttpRequest` |

## Cómo se verifica

- `npm run lint`: `ng lint` sobre `src/**/*.ts`, `src/**/*.html`, `tools/**/*.mts`, `e2e/**/*.ts` y configs raíz.
- `npm run lint:architecture`: `tools/architecture-lint/architecture-lint.spec.mts` linta fixtures de `tools/architecture-lint/fixtures/` como si vivieran en rutas virtuales de `src/`; exige el error esperado en violaciones y ningún error de arquitectura en casos permitidos. También corre en `npm test` (`test:tooling`). Los fixtures están excluidos del lint normal.

## Cambiar la matriz

Ampliar un permiso es una decisión explícita. En el mismo cambio: `eslint.config.js`, fixture negativo y positivo en `tools/architecture-lint/`, y esta reference.

## Revisión manual (lo que el lint no cubre)

- Toast, diálogos o componentes dentro de adapters.
- DTOs expuestos en tipos de `core` o en templates.
- Reglas de negocio dentro de `data` o de un template.
