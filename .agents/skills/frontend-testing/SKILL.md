---
name: frontend-testing
description: "Trigger: frontend test, test Angular, Vitest, Playwright, E2E web, test de use case o adapter frontend. Pruebas del frontend hexagonal por capa."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "1.4"
---

# Testing Frontend — Market Real

## Activation Contract

Complementa a `hexaclean-architecture` en `MarketjoyaFront/`. Runners: Vitest vía `@angular/build:unit-test`, Vitest de tooling (`vitest.tooling.config.mts`) y Playwright 1.63. Activa al escribir o revisar tests del frontend (core, adapters, services/stores, guards, tooling, E2E) o al decidir si una feature está lista.

## Hard Rules

- Cobertura por riesgo (venta, caja, stock, auth), no por porcentaje.
- Spec junto al archivo (`<archivo>.spec.ts`). Prohibido `services.spec.ts` horizontal.
- `base`/`core` sin TestBed ni Angular: instancia + fakes de puerto.
- Adapter HTTP: `HttpClient` real + `HttpTestingController`; nunca `vi.fn()` sobre `HttpClient`.
- Errores: asserta `type` + `code` del `AppError`, nunca `description` ([contrato](../backend-architecture/references/api-error-contract.md)). El mapper HTTP cubre los casos de §7.
- Service/store con facade/token fake. Screen que solo cablea no se prueba.
- E2E en `e2e/<flujo>/`: solo flujos críticos, `getByTestId`, sin `waitForTimeout`.
- Tooling (`tools/**/*.spec.mts`) corre en Node, nunca con `ng test`.
- Un comportamiento por test, AAA, nombre `metodo_escenario_resultado`.
- Sin Karma, Jest ni Cypress. No declares verde sin ejecutar el script.

## Decision Gates

| Cambiaste | Cómo | Comando |
|---|---|---|
| `base`, tipo, puerto, caso de uso | Unitario con fakes | `npm run test:unit` |
| Token/factory `data` | TestBed mínimo | `npm run test:unit` |
| Adapter HTTP/storage, mapper de error | `HttpTestingController` / fake storage | `npm run test:unit` |
| Service, store, guard | Fake del token `in` / TestBed del guard | `npm run test:unit` |
| Flujo crítico | Playwright | `npm run e2e` |
| `eslint.config.js` | Fixture en `tools/architecture-lint/` | `npm run lint:architecture` |
| Generador de tokens / Tailwind | Vitest Node | `npm run test:tooling` |

## Execution Steps

1. Identifica capa y riesgo; lee `references/strategy.md` y la reference de la capa.
2. Copia `templates/`; itera con `npm run test:watch`.
3. Aplica el checklist de la capa.
4. Flujo crítico: `references/e2e.md` + `npm run e2e`.
5. Cierre: `npm run verify` (`checklists/verification.md`).

## Output Contract

- Capa, archivos y comportamientos (éxito, error con `type` + `code`, loading).
- Fakes o `HttpTestingController` usados.
- E2E corrido o "no crítico".
- Comandos con resultado, o motivo de skip.
- Huecos de dinero/auth/permiso, incluidos los "Pendiente de decisión".

## References

- [references/strategy.md](references/strategy.md), [references/conventions.md](references/conventions.md)
- [references/core.md](references/core.md), [references/data.md](references/data.md), [references/infrastructure.md](references/infrastructure.md), [references/ui.md](references/ui.md), [references/e2e.md](references/e2e.md)
- [templates/](templates/), [checklists/](checklists/)
- [../backend-architecture/references/api-error-contract.md](../backend-architecture/references/api-error-contract.md)
- [../hexaclean-architecture/SKILL.md](../hexaclean-architecture/SKILL.md), [../design-system/SKILL.md](../design-system/SKILL.md), [../backend-testing/SKILL.md](../backend-testing/SKILL.md), [../test-e2e/SKILL.md](../test-e2e/SKILL.md)
