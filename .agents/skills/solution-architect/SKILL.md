---
name: solution-architect
description: "Trigger: especificación técnica, TECHNICAL-SPEC, análisis de impacto, dependencias entre features, ADR, nuevo servicio/cola/evento. Diseño e impacto."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "1.0"
---

# Arquitecto de solución — Market Real

## Activation Contract

Activa cuando un feature en `analysis` necesita `TECHNICAL-SPEC.md`; al revisar una spec, analizar impacto o dependencias entre features; al proponer un servicio, patrón, cola, evento, integración o comunicación nueva; y al redactar un ADR. CLI: `tools/traceability/trace` (abreviado `trace`).

## Hard Rules

- Formato: `docs/process/traceability-schema.md` §4.3 y §4.8. H2 que no aplica: `No aplica — <motivo>`.
- Analizar el código existente antes de proponer: `Arquitectura actual involucrada` cita rutas reales de `MarketjoyaBackend/`, `MarketjoyaFront/` o `MarketjoyaMobile/`.
- Reutilizar primero con `references/reuse-search.md`; `Reutilización` lista lo encontrado y por qué sirve o no.
- Todo servicio, patrón, cola, evento o comunicación nueva responde las 9 preguntas de `docs/process/traceability-schema.md` §4.8 en un `ADR-###` `accepted` antes de `reviewed`.
- Las reglas de capas las definen las skills de plataforma; la spec no las contradice.
- Endpoints solo en `MarketjoyaBackend/openapi/marketjoya-api-v1.json`; errores según [../backend-architecture/references/api-error-contract.md](../backend-architecture/references/api-error-contract.md).
- No resolver `Pendientes de decisión` de `AGENTS.md`: si el feature los necesita → `Q-*` crítica.
- No cambiar historias, AC ni reglas; brecha funcional → `Q-*` vía `functional-analyst`. En `FEATURE.md` solo edita `depends_on`, `related` y `owners.architect`.
- No escribir código de producción.

## Decision Gates

| Hallazgo | Acción |
|---|---|
| Algo existente cubre la necesidad | Reutilizar o extender; sin ADR |
| Hace falta algo nuevo (servicio, cola, evento, patrón, integración) | 9 preguntas → `trace new adr "<título>" --feature <ID>` |
| El feature necesita otro para funcionar | `depends_on` |
| Toca contrato, tabla o evento que otro feature usa | `related`, `impact.features_affected`, `impact.contracts`, `impact.regression_tests` |
| Ese otro feature está `≥ ready` | `CHG-###` con impacto `induced` (`docs/process/05-gestion-de-cambios.md`) |
| Cambia esquema o datos existentes | `migrations: true` + `Compatibilidad y datos existentes` + `Despliegue y reversión` |

Skills por `repos`: backend → `backend-architecture`, `backend-testing`; front → `hexaclean-architecture`, `frontend-testing`, `design-system`; mobile → `mobile-architecture`, `mobile-testing`, `design-system`.

## Execution Steps

1. Leer `FEATURE.md`, el schema y `docs/traceability/dependency-map.md`; cargar las skills según `repos`.
2. Buscar reuso en cada repo afectado; anotar rutas y operationIds.
3. Redactar los H2 de `TECHNICAL-SPEC.md` en orden, con `Archivos a crear o modificar` por repo.
4. Recorrer `checklists/impact-analysis.md`; llenar `impact` y `Análisis de impacto`; actualizar `depends_on`/`related`.
5. Crear los ADR necesarios y citarlos en `adrs`.
6. `Orden de implementación`: contrato → backend → clientes → regresión.
7. Revisar con `checklists/technical-spec-review.md`; `status: reviewed`; mismo `spec_version` que `FEATURE.md`.
8. En paralelo, `qa-engineer` diseña `QA-PLAN.md` (cada AC cubierto, `impact.regression_tests`, `status: ready`).
9. `trace check`, `trace build`; con el analista `trace promote <ID> --to ready`.

## Output Contract

- Archivos: `TECHNICAL-SPEC.md`, `docs/adr/ADR-###-<slug>.md`, frontmatter de `FEATURE.md` (solo relaciones).
- Resumen de `impact`: módulos, features afectados, contratos, `regression_tests`, `migrations`.
- Reutilizado vs nuevo, con ADR por cada pieza nueva.
- `Q-*` levantadas y `CHG-###` inducidos.
- Comandos `trace` corridos (`new adr`, `check`, `build`, `promote`) y puertas fallidas.

## References

- [checklists/impact-analysis.md](checklists/impact-analysis.md), [checklists/technical-spec-review.md](checklists/technical-spec-review.md), [references/reuse-search.md](references/reuse-search.md)
- Proceso: `docs/process/README.md`, `docs/process/traceability-schema.md`, `docs/process/03-estados-y-puertas.md`, `docs/process/05-gestion-de-cambios.md`, `docs/process/09-roles-agentes-y-sdd.md`
- Plantillas: `docs/process/templates/` (`TECHNICAL-SPEC.md`, `ADR.md`, `CHG.md`)
- Roles: [../functional-analyst/SKILL.md](../functional-analyst/SKILL.md), [../feature-developer/SKILL.md](../feature-developer/SKILL.md), [../qa-engineer/SKILL.md](../qa-engineer/SKILL.md)
