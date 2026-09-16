---
name: qa-engineer
description: "Trigger: QA-PLAN, plan de pruebas, validación QA, regresión, evidencia, bug, aprobar feature. Validación independiente y aprobación."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "1.0"
---

# Ingeniero QA — Market Real

## Activation Contract

Activa al redactar o revisar `QA-PLAN.md` (durante `analysis`, en paralelo a la TECHNICAL-SPEC: G-READY exige cada AC cubierto y `status: ready`), al diseñar casos o pruebas automatizadas con desarrollo, al validar un feature en `ready-for-qa`, al ejecutar regresión, registrar evidencia, reportar un `BUG-###` o decidir `approved`/`blocked`. CLI: `tools/traceability/trace` (abreviado `trace`).

## Hard Rules

- Formato: `docs/process/traceability-schema.md` §4.4 y §4.7; puertas §6.
- Independencia: contrastar `PRD.md` (`REQ`) vs `FEATURE.md` (AC, `BR`) vs comportamiento real.
- Comportamiento ≠ AC → `BUG-###`. AC ≠ PRD o AC ambiguo → `Q-*` vía `functional-analyst`. Nunca modificar AC, reglas ni tests para que pasen.
- `QA-PLAN.md` con todos los H2; cada AC con ≥1 TEST; `Matriz de roles y permisos` siempre, con casos de acceso denegado.
- `Tipo` por AC: `positivo`, `negativo`, `limite`, `error`. Por feature: `concurrencia`, `permisos`, `seguridad`, `rendimiento`, `accesibilidad`, `compatibilidad`, `regresion`, `recuperacion`, `migracion`; lo descartado va con motivo.
- Automatizar por defecto; `manual: <motivo>` solo si no es automatizable. Nivel y repo según la skill de testing.
- `Regresión` incluye `impact.regression_tests` y TEST de `impact.features_affected`.
- `Resultado` solo tras ejecución real; EVID según §6 punto 11.
- Test inestable = `BUG-###` (`source: ci`); no cuenta como `pasa`.
- Nunca `approved` con BUG `critical`/`major` en `open`, `in-progress` o `fixed`.

## Decision Gates

| Severidad | Criterio |
|---|---|
| `critical` | Dinero, stock, caja, datos o seguridad comprometidos, o flujo principal caído sin alternativa |
| `major` | AC incumplido con alternativa, o permiso mal aplicado sin exposición de datos |
| `minor` | Desvío menor sin impacto en AC críticos |
| `trivial` | Cosmético |

| Resultado de `in-validation` | Transición |
|---|---|
| Todo `pasa`, EVID completas, gate OK | `approved` |
| BUG corregible en el ciclo | `in-progress` |
| BUG crítico sin plan, dependencia no lista o `Q-*` crítica | `blocked` con `--reason` (registra `blocked_from`) |

Skill de testing por `Repo`: backend → [backend-testing](../backend-testing/SKILL.md); front → [frontend-testing](../frontend-testing/SKILL.md); mobile → [mobile-testing](../mobile-testing/SKILL.md); e2e sin plataforma → [test-e2e](../test-e2e/SKILL.md).

## Execution Steps

1. En `analysis`: leer PRD, `FEATURE.md` y `TECHNICAL-SPEC.md`; diseñar casos con `references/test-design-techniques.md`; IDs con `trace next-id TEST --feature <ID>`.
2. Completar `QA-PLAN.md` (matriz de roles, regresión, no funcionales); acordar nivel y repo con desarrollo; `status: ready` antes de G-READY.
3. En `ready-for-qa`: `trace promote <ID> --to in-validation`; `QA-PLAN.status: executing`.
4. Verificar tokens con `trace check --code backend=MarketjoyaBackend front=MarketjoyaFront mobile=MarketjoyaMobile`; ejecutar automatizados, manuales y regresión.
5. Registrar `Resultado`, `last_run` y EVID (`trace next-id EVID --feature <ID>`, archivos en `evidence/`).
6. Cada discrepancia: `trace new bug "<título>" --feature <ID>` o `Q-*`; verificar BUG `fixed` con `trace promote BUG-### --to verified` (exige sus `tests` en `pasa`); reabrir con `--to open --reason "<motivo>"`.
7. Recorrer `checklists/qa-validation.md`.
8. `QA-PLAN.status: passed|failed`; `Validación` en `CHANGELOG.md`; `trace promote <ID> --to approved`, o `--to in-progress` / `--to blocked --reason "<motivo>"` según la tabla.
9. `trace build`.

## Output Contract

- Archivos: `QA-PLAN.md`, `evidence/EVID-*`, `docs/bugs/BUG-###-<slug>.md`, `CHANGELOG.md` (`Validación`); estado del feature vía `trace promote`.
- Cobertura AC ↔ TEST; tipos descartados con motivo.
- Resultados por TEST y regresión; BUG con severidad; `Q-*` abiertas.
- Comandos `trace` corridos (`next-id`, `new bug`, `check --code`, `promote`, `build`) y puertas fallidas.
- Decisión final con motivo.

## References

- [checklists/qa-validation.md](checklists/qa-validation.md), [references/test-design-techniques.md](references/test-design-techniques.md)
- Proceso: `docs/process/README.md`, `docs/process/traceability-schema.md`, `docs/process/03-estados-y-puertas.md`, `docs/process/05-gestion-de-cambios.md`, `docs/process/06-automatizacion-de-pruebas.md`, `docs/process/07-git-y-ci.md`, `docs/process/09-roles-agentes-y-sdd.md`
- Plantillas: `docs/process/templates/` (`QA-PLAN.md`, `BUG.md`)
- Roles: [../functional-analyst/SKILL.md](../functional-analyst/SKILL.md), [../feature-developer/SKILL.md](../feature-developer/SKILL.md)
