---
name: feature-developer
description: "Trigger: implementar feature, desarrollar FEAT, código de feature, commit de feature. Implementa TECHNICAL-SPEC con TDD hasta ready-for-qa."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "1.0"
---

# Desarrollador de feature — Market Real

## Activation Contract

Activa al implementar un `<MOD>-FEAT-###`, un `CHG-###` o la corrección de un `BUG-###`; al preparar rama, commits o PR de un feature; y al cerrar el desarrollo para QA. Se combina siempre con la skill de arquitectura y la de testing del repo tocado. CLI: `tools/traceability/trace` (abreviado `trace`).

## Hard Rules

- Sin código hasta que `trace promote <ID> --to in-progress` pase (feature en `ready`, o `approved` con CHG/BUG). Estados solo con `trace promote`, nunca editando `status`; también los de CHG/BUG (`trace promote BUG-### --to in-progress|fixed`, `trace promote CHG-### --to implemented`).
- Implementar exactamente `TECHNICAL-SPEC.md` (`Archivos a crear o modificar`, `Orden de implementación`). Nada fuera de la spec.
- Spec incorrecta o incompleta → detener; `Q-*` vía `functional-analyst` (funcional) o `solution-architect` (técnica); cambio de comportamiento en feature `≥ ready` → `CHG-###`.
- En docs solo edita: `owners.developer` de `FEATURE.md`, `Automatización` de `QA-PLAN.md` y la entrada vigente de `CHANGELOG.md`. Nunca reglas, historias, AC ni `Resultado`.
- TDD: test rojo con el token `TEST-<MOD>-###-##` (schema §8) antes del código de producción. La skill de plataforma manda sobre capas y pruebas.
- Git (schema §9) en cada repo tocado (código en su repo, docs en el padre): commit `<type>(<ID>): <asunto>`, rama `<type>/<ID>-<slug>`, PR igual con enlace a la carpeta del feature; validar con `trace lint-commit` / `trace lint-pr`.
- Test inestable = `BUG-###`, nunca reintento silencioso ni `skip`.
- `ready-for-qa` solo con verificación del repo en verde, luego `trace promote <ID> --to ready-for-qa`.

## Decision Gates

| `repos` | Skills | Verificación (desde la carpeta del repo) |
|---|---|---|
| `backend` | [backend-architecture](../backend-architecture/SKILL.md), [backend-testing](../backend-testing/SKILL.md) | `dotnet format --verify-no-changes && dotnet build -c Release && dotnet test` |
| `front` | [hexaclean-architecture](../hexaclean-architecture/SKILL.md), [frontend-testing](../frontend-testing/SKILL.md), [design-system](../design-system/SKILL.md) | `npm run verify` |
| `mobile` | [mobile-architecture](../mobile-architecture/SKILL.md), [mobile-testing](../mobile-testing/SKILL.md), [design-system](../design-system/SKILL.md) | `./gradlew verify` |
| E2E sin plataforma clara | [test-e2e](../test-e2e/SKILL.md) | La de la plataforma elegida |

| Situación | Acción |
|---|---|
| `trace promote` falla | Detener; reportar puertas fallidas |
| TEST del QA-PLAN sin nivel o repo claro | Acordar con `qa-engineer` antes de escribirlo |
| Endpoint o `code` de error no está en la spec/OpenAPI | Detener; `Q-*` al arquitecto |

## Execution Steps

1. `trace promote <ID> --to in-progress`; si pasa, `owners.developer`; crear rama en cada repo tocado.
2. Leer `FEATURE.md` (AC), `TECHNICAL-SPEC.md` y los `TEST-*` de su repo en `QA-PLAN.md`; cargar las skills de la tabla.
3. Por paso del `Orden de implementación`: test rojo con token → código mínimo → verde → refactor.
4. Registrar la ruta real de cada test en `Automatización`.
5. Completar la entrada vigente de `CHANGELOG.md`.
6. Correr la verificación de cada repo tocado; luego `trace check --code backend=MarketjoyaBackend front=MarketjoyaFront mobile=MarketjoyaMobile`.
7. Recorrer `checklists/definition-of-done-dev.md`; `trace build`.
8. `trace promote <ID> --to ready-for-qa`; abrir PR con IDs, puertas y resultado de verificación.

## Output Contract

- Repos, ramas y archivos de código y test por repo.
- `TEST-*` implementados con ruta y nivel; AC cubiertos.
- Comandos de verificación con resultado.
- Docs editados: `FEATURE.md` (estado), `QA-PLAN.md` (`Automatización`), `CHANGELOG.md`.
- Commits y título de PR con su `trace lint-commit` / `trace lint-pr`.
- `trace promote` / `check` / `build` corridos; `Q-*`, `CHG-*` o `BUG-*` levantados.

## References

- [checklists/definition-of-done-dev.md](checklists/definition-of-done-dev.md)
- Proceso: `docs/process/README.md`, `docs/process/traceability-schema.md` (§6, §8, §9), `docs/process/03-estados-y-puertas.md`, `docs/process/05-gestion-de-cambios.md`, `docs/process/06-automatizacion-de-pruebas.md`, `docs/process/07-git-y-ci.md`, `docs/process/09-roles-agentes-y-sdd.md`
- Plantillas: `docs/process/templates/` (`CHANGELOG.md`, `BUG.md`, `CHG.md`)
- Roles: [../solution-architect/SKILL.md](../solution-architect/SKILL.md), [../qa-engineer/SKILL.md](../qa-engineer/SKILL.md)
