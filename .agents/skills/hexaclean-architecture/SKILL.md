---
name: hexaclean-architecture
description: "Trigger: frontend Angular, feature frontend, puerto, adapter, facade, hexagonal, Clean Architecture frontend. Convención hexagonal del frontend de Market Real."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "2.5"
---

# HexaClean — Frontend Market Real

## Activation Contract

Frontend Angular en `MarketjoyaFront/`; rutas `src/...` relativas a esa carpeta. Activa al crear, revisar o explicar capas, feature, operación, puerto, adapter, facade, service, store, ruta, permisos, contrato HTTP, errores o un diff de dependencias.

## Hard Rules

- Capas bajo `src/`: `base` → `core` → `data` / `infrastructure` ← `ui` (+ `environment`). Composición raíz: `src/main.ts` + `src/app.config.ts`. No existe `src/app/`.
- `eslint.config.js` (boundaries) aplica la matriz; no desactives reglas.
- `base`/`core` sin Angular, HTTP, storage, router ni globals. UI sin adapters, `token/out` ni cliente HTTP.
- Puertos de salida por capacidad, no `*RepositoryPort` CRUD.
- Facade ejecuta casos de uso; service orquesta estado y feedback; adapter traduce IO.
- Errores: `GoResult<T, AppError>` del [contrato canónico](../backend-architecture/references/api-error-contract.md). El adapter HTTP es el único traductor ProblemDetails → `AppError`; ramifica por `type` y luego `code`, nunca por `description`.
- HTTP según `MarketjoyaBackend/openapi/marketjoya-api-v1.json` (`/api/v1/...`); no inventes endpoints.
- Autorización real en backend. Sin secretos en el cliente.
- Selector `mr`. Visual solo del pack (`Mr*`, `--mr-*`); Tailwind solo layout.

## Decision Gates

| Situación | Acción |
|---|---|
| Feature / operación nueva | `checklists/new-feature.md` / `new-operation.md` + `templates/` |
| Endpoint, DTO, enum | `references/api-contract.md`; sin operación en el OpenAPI → bloquea |
| Cambiar HTTP/storage | Nuevo adapter, mismos puertos; `checklists/adapter-migration.md` |
| Error, `code`, mensaje o campo inválido | `references/errors-and-results.md` |
| Login, sesión, 401/403, guard | `references/authentication-and-permissions.md` |
| El lint rechaza un import | Corrige la dependencia; ampliar la matriz = decisión explícita |
| Código y skill divergen | Manda el código; en errores, manda el contrato (pendientes en §8); reporta |
| UI / color / componente | `../design-system/SKILL.md` |

## Execution Steps

1. Lee `MarketjoyaFront/AGENTS.md` y el código del bounded context.
2. Localiza el `operationId`; define contrato y errores antes del adapter.
3. core → data → infrastructure → ui con `templates/`.
4. Permisos de ruta y acciones; scope DI = lifecycle del store.
5. Revisión: `checklists/architecture-review.md` (+ `../design-system/checklists/review.md` si hubo UI).
6. Tests: `../frontend-testing/SKILL.md`.
7. `npm run verify` (+ `npm run e2e` si es flujo crítico); `checklists/verification.md`.

## Output Contract

- Árbol por capa bajo `MarketjoyaFront/src/`.
- Puertos `in`/`out` y `operationId` usado o "sin contrato".
- Resultado de `npm run verify` o motivo de no ejecución.
- Puntos "Pendiente de decisión" tocados.
- Bloqueadores primero: dependencia cruzada, `code` perdido o ramificación por `description`, endpoint inventado, secreto en cliente, visual fuera del DS.

## References

- [references/](references/): architecture-map, dependency-rules, vertical-slice, api-contract, data-and-di, errors-and-results, authentication-and-permissions, refresh-session, testing-and-verification
- [templates/](templates/), [checklists/](checklists/)
- [../design-system/SKILL.md](../design-system/SKILL.md), [../frontend-testing/SKILL.md](../frontend-testing/SKILL.md), [../backend-architecture/SKILL.md](../backend-architecture/SKILL.md), [../mobile-architecture/SKILL.md](../mobile-architecture/SKILL.md)
