# Checklist: revisión de TECHNICAL-SPEC

Antes de `status: reviewed`. Falla cualquier casilla → la spec sigue en `draft`.

## Forma

- [ ] Frontmatter §4.3 completo; `spec_version` igual a `FEATURE.md`.
- [ ] Los 21 H2 obligatorios presentes y en orden; ninguno vacío (`No aplica — <motivo>` si corresponde).
- [ ] `impact.modules_direct` no vacío; `adrs` existentes y no `superseded`/`deprecated`.

## Fidelidad funcional

- [ ] Cada AC de `FEATURE.md` tiene dónde se implementa (sección de repo + archivo).
- [ ] Ninguna regla, validación o estado nuevo que no esté en `FEATURE.md`.
- [ ] Brechas detectadas registradas como `Q-*` (no resueltas por supuesto técnico).
- [ ] `Pendientes de decisión` de `AGENTS.md` no resueltos en la spec.

## Diseño

- [ ] `Arquitectura actual involucrada` con rutas reales leídas.
- [ ] `Reutilización` con resultado de la búsqueda (encontrado / descartado y por qué).
- [ ] Capas y dependencias según la skill de arquitectura de cada repo.
- [ ] Endpoints en el OpenAPI; errores con `code` según el contrato de errores; ProblemDetails.
- [ ] Autorización real en backend; permisos nombrados.
- [ ] DTO ≠ persistencia ≠ dominio ≠ UI model.
- [ ] Concurrencia, idempotencia y transacciones explícitas donde hay dinero, stock o caja.
- [ ] Migraciones con compatibilidad de datos existentes y reversión.
- [ ] `Archivos a crear o modificar` por repo y `Orden de implementación` ejecutable.
- [ ] Riesgos con mitigación.

## Las 9 preguntas para todo lo nuevo

- [ ] Cada servicio, patrón, cola, evento, integración o comunicación nueva tiene ADR con las 9 preguntas literales de `docs/process/traceability-schema.md` §4.8 respondidas en `Justificación de lo nuevo`.
- [ ] La respuesta a "¿por qué no puede reutilizarse?" cita comandos y rutas de [../references/reuse-search.md](../references/reuse-search.md).

## Cierre

- [ ] `QA-PLAN.md` cubre cada AC con ≥1 TEST, incluye `impact.regression_tests` y está en `status: ready` (lo diseña QA durante `analysis`).
- [ ] `trace check` sin errores; `trace promote <ID> --to ready` ejecutado con el analista.
