# Checklist: validación QA

## Plan (antes de G-READY)

- [ ] Frontmatter §4.4 completo; `spec_version` igual a `FEATURE.md`.
- [ ] Los 13 H2 obligatorios presentes y con contenido.
- [ ] Cada AC referenciado por ≥1 TEST en `Casos de prueba`.
- [ ] Por AC: `positivo`, `negativo`, `limite` y `error` considerados (o descartados con motivo).
- [ ] `Matriz de roles y permisos`: cada rol × cada acción del feature, con resultado esperado (permitido / denegado / oculto).
- [ ] No funcionales evaluados: concurrencia, seguridad, rendimiento, accesibilidad, compatibilidad (versiones Android, navegadores), recuperación (red, reinicio, hardware), migración.
- [ ] `Regresión` con `impact.regression_tests` y TEST de `impact.features_affected`.
- [ ] Cada TEST con `Tipo`, `Nivel` y `Repo` válidos (§4.4); nivel acordado con desarrollo.
- [ ] `manual:` solo con motivo real.
- [ ] `Datos de prueba` y `Entornos` concretos (roles, cajas, productos, montos).
- [ ] Contraste PRD ↔ FEATURE hecho; discrepancias como `Q-*`.
- [ ] `QA-PLAN.status: ready` durante `analysis` (condición de G-READY punto 5).

## Entrada a validación

- [ ] Feature en `ready-for-qa`; `trace promote <ID> --to in-validation` ejecutado.
- [ ] Toda ruta de `Automatización` existe y contiene su token (`trace check --code …`).
- [ ] CI del PR en verde; versión o commit bajo prueba anotado.

## Ejecución

- [ ] Automatizados corridos en el entorno declarado; resultado por TEST.
- [ ] Manuales ejecutados con pasos reproducibles.
- [ ] Regresión ejecutada; resultado por fila.
- [ ] Comportamiento real comparado contra AC y contra PRD (no solo contra el test).
- [ ] Permisos probados con usuarios reales de cada rol, incluido acceso directo por API sin UI.
- [ ] Doble envío, concurrencia y pérdida de red probados donde hay dinero, stock o caja.
- [ ] Ningún test marcado `pasa` tras reintentos; inestables reportados como BUG.

## Evidencia y defectos

- [ ] ≥1 EVID por TEST `e2e` automatizado y por cada TEST `manual`; archivos en `evidence/` o URL de CI.
- [ ] `last_run` actualizado.
- [ ] Cada BUG con severidad, `tests`, `criteria`, pasos, esperado vs obtenido y `Fuente de verdad`.
- [ ] BUG corregidos re-ejecutados y pasados a `verified` con `trace promote BUG-### --to verified` (nunca editando `status`).

## Cierre

- [ ] Sin BUG `critical`/`major` en `open`, `in-progress` o `fixed`.
- [ ] `depends_on` en `approved`/`released`, o en `ready-for-qa`/`in-validation` con el mismo `target_release` (§6 punto 13).
- [ ] CHG que listan el feature con entrada en `CHANGELOG.md`; `Validación` actualizada.
- [ ] `QA-PLAN.status` en `passed` o `failed`.
- [ ] `trace promote <ID> --to approved`; si no procede, `trace promote <ID> --to blocked --reason "<motivo>"` (registra `blocked_from`) o `--to in-progress`.
- [ ] `trace build` corrido.
