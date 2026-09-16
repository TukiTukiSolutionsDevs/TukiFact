# Roles, agentes de IA y SDD

**Un agente trabaja con un único rol a la vez, lee solo los artefactos de entrada de ese rol, escribe solo sus artefactos de salida y termina con un mensaje de traspaso.** Los documentos de `docs/features/` son la memoria compartida y la fuente de verdad; la memoria de sesión (engram) solo guarda progreso y contexto de trabajo.

## Camino rápido para un agente

1. Identifica el rol pedido y carga su skill.
2. Lee el mensaje de traspaso recibido y los archivos de entrada del rol.
3. Escribe solo los artefactos de salida; ejecuta `trace check` y la puerta del rol.
4. Guarda el progreso con el topic key del feature.
5. Entrega el mensaje de traspaso al siguiente rol.

## Roles

| Rol | Skill | Lee | Escribe |
|---|---|---|---|
| Analista funcional | `functional-analyst` | Maquetado, `VIEWS.md`, `FUNCTIONAL-ANALYSIS.md`, `PRD.md`, `modules.yaml`, features relacionados | `VIEWS.md`, `FUNCTIONAL-ANALYSIS.md`, `PRD.md`, `FEATURE.md`, `CHG` (motivo, requisitos) |
| Arquitecto de solución | `solution-architect` | `FEATURE.md`, specs y `impact` de features relacionados, skills de arquitectura (`backend-architecture`, `hexaclean-architecture`, `mobile-architecture`), OpenAPI, contrato de errores, ADR | `TECHNICAL-SPEC.md`, `ADR`, `modules.yaml`, `CHG` (impacto técnico, features afectados) |
| Desarrollador | `feature-developer` | `FEATURE.md`, `TECHNICAL-SPEC.md`, `QA-PLAN.md` (casos), skills de arquitectura y testing del repo | Código y pruebas en repos hijos; columna `Automatización`; `status` `in-progress` / `ready-for-qa`; `CHANGELOG` (archivos técnicos) |
| QA | `qa-engineer` | `FEATURE.md`, `PRD.md`, `TECHNICAL-SPEC.md` (`impact`), resultados de CI | `QA-PLAN.md` (diseño durante `analysis` hasta `status: ready`; luego resultados), `evidence/`, `BUG`, `status` `in-validation` / `approved`, `CHANGELOG` (validación) |

Todo cambio de `status` de un feature se hace con `trace promote <ID> --to <estado>`; `trace gate` sirve para consultar sin modificar.

Las skills viven en `.agents/skills/<skill>/SKILL.md` y se enrutan desde [AGENTS.md](../../AGENTS.md).

## Acciones prohibidas

| Rol | Prohibido |
|---|---|
| Analista | Inventar reglas no confirmadas (usar `Q`/`ASM`); decidir tecnología; editar `TECHNICAL-SPEC.md` o código; marcar `ready` sin visto bueno de arquitecto y QA |
| Arquitecto | Proponer un componente, servicio, cola, evento o patrón nuevo sin buscar antes reutilización y documentarla en `Reutilización`; introducir algo nuevo sin ADR con las 9 preguntas; cambiar AC o reglas de `FEATURE.md`; resolver decisiones marcadas como pendientes en `AGENTS.md` |
| Desarrollador | Cambiar reglas, AC o historias de `FEATURE.md`; empezar un feature que no está `ready`; inventar endpoints fuera del OpenAPI; omitir el token `TEST-…`; marcar `approved` |
| QA | Editar AC, reglas o spec para que la implementación pase; aprobar con BUG `critical`/`major` abierto; reintentar hasta verde; validar su propio código |
| Todos | Editar `docs/traceability/`; editar `status`, `blocked_reason` o `blocked_from` a mano (usar `trace promote`); reutilizar o renumerar IDs; tocar artefactos de otro rol (se propone el cambio en el traspaso) |

## Mensaje de traspaso

```markdown
## Traspaso <rol origen> → <rol destino>

- Feature: PAY-FEAT-001 · spec_version 1.0.0 · estado: analysis → ready
- Puerta verificada: `trace gate PAY-FEAT-001 --to ready` → OK (o lista de puertas fallidas)
- Artefactos escritos: docs/features/payments/PAY-FEAT-001-register-payment/TECHNICAL-SPEC.md
- IDs creados: ADR-002; IDs modificados: —
- Decisiones: reutiliza `client_mutation_id` existente (ADR-002)
- Abierto: Q-PAY-001-04 (no crítica), ASM-PAY-001-01 pendiente de negocio
- Riesgos: migración de datos en schema payments
- Siguiente acción esperada: QA declara TEST para AC-PAY-001-01…06
- Memoria: feature/PAY-FEAT-001/tech-spec
```

Reglas: el mensaje referencia rutas e IDs, no copia contenido; lo que no está en un archivo no existe para el siguiente rol.

## Memoria de sesión (engram)

| Topic key | Contenido | Escribe |
|---|---|---|
| `feature/<ID>/analysis` | Hallazgos del análisis, fuentes consultadas | Analista |
| `feature/<ID>/tech-spec` | Alternativas descartadas, búsqueda de reutilización | Arquitecto |
| `feature/<ID>/apply-progress` | Tareas hechas y pendientes por repo | Desarrollador |
| `feature/<ID>/qa-report` | Ejecuciones, fallas, BUG abiertos | QA |
| `feature/<ID>/handoff/<origen>-<destino>` | Último mensaje de traspaso | Rol origen |
| `change/<CHG-ID>/impact` | Análisis de impacto del cambio | Arquitecto |

Recuperación tras perder contexto: leer primero los archivos del feature y `docs/traceability/`; la memoria complementa, nunca reemplaza al documento.

## Relación con las fases SDD

Las fases SDD se usan tal como están definidas; aquí solo se indica **qué artefacto del proceso cumple cada fase**. El almacén de artefactos es `docs/features/` (y `docs/changes/` para CHG): no se duplican en `openspec/`.

| Fase SDD | Rol | Artefacto del proceso | Puerta / estado |
|---|---|---|---|
| `sdd-explore` | Analista / arquitecto | `FUNCTIONAL-ANALYSIS.md`, `Arquitectura actual involucrada`, `Reutilización` | — |
| `sdd-propose` | Analista | `FEATURE.md` (`Objetivo`, `Problema`, `Alcance`) o `CHG` (`Motivo`) | `draft` → `analysis` |
| `sdd-spec` | Analista | `FEATURE.md` (US, AC, BR, validaciones, errores) | Puntos 2, 3 y 7 de G-READY |
| `sdd-design` | Arquitecto | `TECHNICAL-SPEC.md`, `impact`, ADR | Puntos 4 y 6 de G-READY |
| `sdd-tasks` | Arquitecto / QA | `Orden de implementación` + `Casos de prueba` de `QA-PLAN.md` (`status: ready`) | G-READY → `trace promote --to ready` |
| `sdd-apply` | Desarrollador | Código, pruebas con token, `Automatización` | `in-progress` → G-QA |
| `sdd-verify` | QA | `QA-PLAN.md` resultados, regresión, `evidence/` | `in-validation` → G-APPROVED |
| `sdd-archive` | QA / release | `CHANGELOG.md`, `released_in`, `trace build` | G-RELEASED |

- El nombre del cambio SDD es el ID: `PAY-FEAT-001` o `CHG-004`.
- `sdd-verify` del desarrollador no sustituye a la validación de QA: la independencia de QA se mantiene.
- Si la sesión SDD usa engram, los topic keys `sdd/<ID>/…` guardan solo progreso; los artefactos finales se escriben en `docs/`.
