# Ejemplo completo: apertura de caja en POS

> **Ejemplo ilustrativo.** Las reglas de negocio reales de Market Real **no están definidas**. Todo lo que aparece aquí (montos, umbral de S/ 500.00, módulos `CashRegister` y `Reports`, endpoints, pruebas, commits, evidencias, fechas) es ficticio. Las reglas se presentan como `ASM-…` y `Q-…` "confirmadas en el ejemplo" para mostrar cómo funciona el mecanismo, no como decisiones del producto. Donde el ejemplo necesita algo que `AGENTS.md` marca como pendiente de decisión (transporte de `client_mutation_id`, worker de sync, contrato de auth), se indica en el documento correspondiente.

Este árbol tiene la misma forma que `docs/` y recorre el proceso completo de [docs/process/](../../README.md): del maquetado a dos releases, pasando por un bug encontrado por QA, un ADR y un cambio (CHG) con impacto inducido en otro feature. Todos los estados se movieron con `tools/traceability/trace` y las salidas que se muestran son reales.

## Camino rápido

```bash
R=docs/process/examples/pos-cash-opening
tools/traceability/trace check --docs $R                          # OK: 0 errores, 0 avisos
tools/traceability/trace build --docs $R --check                  # OK: traceability/ al día
tools/traceability/trace gate CASH-FEAT-001 --to released --docs $R   # OK: 0 errores, 0 avisos
```

Para leerlo en orden: [PRD](product/PRD.md) → [CASH-FEAT-001](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/FEATURE.md) → [su especificación](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/TECHNICAL-SPEC.md) → [su plan de QA](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/QA-PLAN.md) → [CHG-001](changes/CHG-001-aprobacion-supervisor-apertura-sobre-umbral.md) → [matriz](traceability/matrix.md).

## Qué contiene

| Artefacto | Rol dueño | Qué muestra |
|---|---|---|
| [product/modules.yaml](product/modules.yaml) | Arquitecto | Módulos `AUTH`, `CASH`, `REPORT` y su correspondencia con módulos backend |
| [product/VIEWS.md](product/VIEWS.md) | Analista | VIEW-001 (POS), VIEW-002 (aprobación web), VIEW-003 (reporte), FLOW-001 |
| [product/FUNCTIONAL-ANALYSIS.md](product/FUNCTIONAL-ANALYSIS.md) | Analista | Inferido vs por confirmar por vista |
| [product/PRD.md](product/PRD.md) | Analista | REQ-001 a REQ-008 (tres no funcionales), Q-PRD-001 y Q-PRD-002 resueltas |
| [product/RELEASES.md](product/RELEASES.md) | Responsable de release | REL-2026.09.1 y REL-2026.10.1 |
| [AUTH-FEAT-001](features/auth/AUTH-FEAT-001-iniciar-sesion-en-pos/FEATURE.md) | Todos | Feature mínimo, dependencia de CASH; `released` 1.0.0 |
| [CASH-FEAT-001](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/FEATURE.md) | Todos | Ejemplo completo: 3 US, 9 AC, 8 BR, 4 ASM, 24 TEST, 7 EVID; `released` 1.1.0 |
| [REPORT-FEAT-001](features/reports/REPORT-FEAT-001-reporte-de-sesiones-de-caja/FEATURE.md) | Todos | Feature relacionado con cambio inducido; `released` 1.0.1 |
| [ADR-001](adr/ADR-001-sesion-unica-por-caja-e-idempotencia.md) | Arquitecto | Índice único parcial + idempotencia + primer consumer entre módulos, con las 9 preguntas |
| [BUG-001](bugs/BUG-001-doble-toque-envia-dos-aperturas.md) | QA | Doble toque, `major`, `closed`, con `Fuente de verdad` |
| [CHG-001](changes/CHG-001-aprobacion-supervisor-apertura-sobre-umbral.md) | Analista + arquitecto + QA | Los 13 pasos; directo en CASH (minor), inducido en REPORT (patch) |
| [traceability/](traceability/matrix.md) | Generado | `matrix.md`, `dependency-map.md`, `open-questions.md` |

## Línea de tiempo

| Fecha | CASH-FEAT-001 | REPORT-FEAT-001 | AUTH-FEAT-001 | Otros |
|---|---|---|---|---|
| 2026-08-10 | — | — | — | Producto: VIEWS, análisis funcional, PRD 0.1.0, módulos |
| 2026-08-11 | draft | draft | draft → analysis | `trace new feature` ×3 |
| 2026-08-12 | analysis | analysis | ready → in-progress | — |
| 2026-08-13/14 | puerta `ready` **falla**, se corrige | — | — | ADR-001 `accepted` |
| 2026-08-17/19 | ready → in-progress | ready → in-progress | ready-for-qa (08-18), in-validation (08-19) | — |
| 2026-08-21 | — | — | approved | — |
| 2026-08-26/27 | puerta `ready-for-qa` **falla**, se corrige → ready-for-qa → in-validation → in-progress | ready-for-qa | — | BUG-001 `open` |
| 2026-08-28/31 | ready-for-qa → in-validation; puerta `approved` **falla**, se corrige → approved | in-validation | — | BUG-001 `fixed` → `verified` |
| 2026-09-01 | — | approved | — | — |
| 2026-09-07 | released 1.0.0 | released 1.0.0 | released 1.0.0 | REL-2026.09.1; BUG-001 `closed` |
| 2026-09-14/17 | released → analysis | released → analysis | — | CHG-001 proposed → analysis → approved; PRD 0.2.0 (REQ-008) |
| 2026-09-18 | ready → in-progress (1.1.0) | ready → in-progress (1.0.1) | — | — |
| 2026-09-24/25 | ready-for-qa → in-validation | ready-for-qa → in-validation | — | CHG-001 `implemented` |
| 2026-09-29 | approved | approved | — | CHG-001 `verified` |
| 2026-10-02 | released 1.1.0 | released 1.0.1 | — | REL-2026.10.1; CHG-001 `released` |

## Recorrido por etapas

Convenciones de los comandos:

- `R=docs/process/examples/pos-cash-opening` y `trace` = `tools/traceability/trace`.
- `--templates docs/process/templates` aparece en los comandos por claridad; desde la corrección del CLI no es necesario, porque por defecto usa las plantillas del repositorio aunque `--docs` apunte al ejemplo.
- `--date` es la opción global del CLI que fija la fecha escrita por `promote` y `new`; aquí simula la línea de tiempo. En un proyecto real se omite.
- En las salidas, `…/CASH-FEAT-001/` abrevia `docs/process/examples/pos-cash-opening/features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/`.

### 1. Producto (analista y arquitecto, 2026-08-10)

El analista registra las vistas del maquetado, las analiza separando inferido de por confirmar y redacta el PRD. Lo que el maquetado no dice queda como `Q-PRD`: Q-PRD-001 (¿apertura sin conexión?) se resuelve y da REQ-005; Q-PRD-002 (valor del umbral de aprobación) queda **abierta y crítica**, por eso la aprobación de supervisor queda fuera de CASH-FEAT-001 1.0.0. El arquitecto registra los módulos.

```text
$ trace next-id VIEW --docs $R          →  VIEW-001
$ trace next-id REQ --docs $R           →  REQ-001
$ trace next-id Q-PRD --docs $R         →  Q-PRD-001
$ trace check --docs $R
OK: 0 errores, 0 avisos
```

### 2. Features en borrador (analista, 2026-08-11)

```text
$ trace new feature AUTH "Iniciar sesión en POS" --templates docs/process/templates --date 2026-08-11 --docs $R
creado: docs/process/examples/pos-cash-opening/features/auth/AUTH-FEAT-001-iniciar-sesion-en-pos
$ trace new feature CASH "Abrir sesión de caja" --templates docs/process/templates --date 2026-08-11 --docs $R
creado: docs/process/examples/pos-cash-opening/features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja
$ trace new feature REPORT "Reporte de sesiones de caja" --templates docs/process/templates --date 2026-08-11 --docs $R
creado: docs/process/examples/pos-cash-opening/features/reports/REPORT-FEAT-001-reporte-de-sesiones-de-caja

# sin --templates dentro de un ejemplo:
$ trace new feature CASH "Abrir sesión de caja" --docs $R
error de uso: faltan plantillas en docs/process/examples/pos-cash-opening/process/templates: FEATURE.md, TECHNICAL-SPEC.md, QA-PLAN.md, CHANGELOG.md
```

El analista completa `FEATURE.md` (actores, flujos, estados, BR, validaciones, mensajes con `code` del contrato de errores, US y AC) y lo pasa a análisis:

```text
$ trace promote CASH-FEAT-001 --to analysis --date 2026-08-12 --docs $R
promote CASH-FEAT-001 → analysis
OK: 0 errores, 0 avisos
```

### 3. Especificación y plan de QA en paralelo — primera puerta fallida (2026-08-13/14)

El arquitecto crea ADR-001 y redacta la especificación (aún `draft`); QA diseña el plan (aún `draft`) y asocia por error la prueba de idempotencia al AC de modo sin conexión. Q-CASH-001-01 (dos dispositivos a la vez) sigue abierta.

```text
$ trace new adr "Sesión única por caja e idempotencia de apertura" --feature CASH-FEAT-001 --slug sesion-unica-por-caja-e-idempotencia --templates docs/process/templates --date 2026-08-13 --docs $R
creado: docs/process/examples/pos-cash-opening/adr/ADR-001-sesion-unica-por-caja-e-idempotencia.md

$ trace gate CASH-FEAT-001 --to ready --docs $R
gate CASH-FEAT-001 → ready
…/CASH-FEAT-001/FEATURE.md:145: error [G-READY-5] CASH-FEAT-001: AC-CASH-001-05 no está cubierto por ningún TEST en «Casos de prueba»
…/CASH-FEAT-001/FEATURE.md:172: error [G-READY-3] CASH-FEAT-001: Q-CASH-001-01 es crítica y sigue abierta
…/CASH-FEAT-001/QA-PLAN.md:4: error [G-READY-5] CASH-FEAT-001: QA-PLAN.status es draft; QA debe dejar el plan en ready durante analysis
…/CASH-FEAT-001/TECHNICAL-SPEC.md:4: error [G-READY-4] CASH-FEAT-001: TECHNICAL-SPEC.status es `draft`; se requiere reviewed o approved
FALLA: 4 errores, 0 avisos
```

Corrección, cada rol en su artefacto:

| Puerta | Quién | Qué hizo |
|---|---|---|
| G-READY-3 | Analista | Resolvió Q-CASH-001-01 con la jefatura de caja (en el ejemplo) y reescribió AC-CASH-001-03 como caso de concurrencia; confirmó ASM-CASH-001-02 |
| G-READY-4 | Arquitecto | Aceptó ADR-001 (con las 9 preguntas) y marcó la especificación `reviewed` |
| G-READY-5 | QA | Asoció TEST-CASH-001-09 y TEST-CASH-001-14 a AC-CASH-001-05 y dejó el plan en `ready` |

```text
$ trace gate CASH-FEAT-001 --to ready --docs $R
OK: 0 errores, 0 avisos
$ trace promote CASH-FEAT-001 --to ready --date 2026-08-17 --docs $R
promote CASH-FEAT-001 → ready
OK: 0 errores, 0 avisos
$ trace promote CASH-FEAT-001 --to in-progress --date 2026-08-17 --docs $R
OK: 0 errores, 0 avisos
```

### 4. Desarrollo y pruebas automatizadas (2026-08-17 a 2026-08-26)

Los desarrolladores implementan en backend y mobile con TDD, escriben la ruta de cada prueba en `Automatización` y completan `Archivos técnicos` del CHANGELOG. Olvidar la ruta del flujo Maestro bloquea G-QA:

```text
$ trace gate CASH-FEAT-001 --to ready-for-qa --docs $R
…/CASH-FEAT-001/QA-PLAN.md:88: error [G-QA-8] CASH-FEAT-001: TEST-CASH-001-14 (mobile) no tiene ruta de automatización
FALLA: 1 errores, 0 avisos
```

Con `maestro/cash-register/open-cash-session.yaml` en la tabla: `trace promote CASH-FEAT-001 --to ready-for-qa --date 2026-08-26` → OK.

### 5. Validación QA, BUG-001 y segunda puerta fallida (2026-08-27 a 2026-08-31)

QA pasa el feature a `in-validation`. El flujo Maestro con doble toque falla: el POS muestra "La caja ya tiene una sesión abierta" aunque la caja quedó abierta. QA contrasta con la fuente de verdad (AC-CASH-001-05 dice "sin mensaje de error"), abre un BUG y devuelve el feature a desarrollo. No edita el AC.

```text
$ trace next-id BUG --docs $R                            →  BUG-001
$ trace new bug "Doble toque en Abrir caja envía dos aperturas" --feature CASH-FEAT-001 --slug doble-toque-envia-dos-aperturas --templates docs/process/templates --date 2026-08-27 --docs $R
creado: docs/process/examples/pos-cash-opening/bugs/BUG-001-doble-toque-envia-dos-aperturas.md
$ trace next-id TEST --feature CASH-FEAT-001 --docs $R   →  TEST-CASH-001-16
$ trace promote CASH-FEAT-001 --to in-progress --date 2026-08-27 --docs $R
OK: 0 errores, 0 avisos
```

El fix (`fix(BUG-001): …`) agrega TEST-CASH-001-16. QA re-ejecuta, registra resultados y evidencias, pero intenta aprobar con el BUG todavía `fixed` y el CHANGELOG `pendiente`:

```text
$ trace gate CASH-FEAT-001 --to approved --docs $R
gate CASH-FEAT-001 → approved
docs/process/examples/pos-cash-opening/bugs/BUG-001-doble-toque-envia-dos-aperturas.md:6: error [G-APPROVED-12] CASH-FEAT-001: BUG-001 (major, fixed) sigue abierto
…/CASH-FEAT-001/CHANGELOG.md:20: error [G-APPROVED-14] CASH-FEAT-001: la entrada más reciente [1.0.0] tiene Validación `pendiente`; se requiere aprobado
FALLA: 2 errores, 0 avisos
```

QA verifica el fix con TEST-CASH-001-14 y TEST-CASH-001-16 (EVID-CASH-001-03), pasa BUG-001 a `verified` y marca `Validación = aprobado`: `trace promote CASH-FEAT-001 --to approved --date 2026-08-31` → OK.

> **Error de orden que conviene evitar.** Crear un archivo `evidence/EVID-…md` antes de declarar su fila en `Evidencias` rompe todo `trace promote` del feature, porque el CLI escanea los `.md` de `evidence/` en busca de IDs:
>
> ```text
> $ trace promote CASH-FEAT-001 --to ready-for-qa --date 2026-08-26 --docs $R
> …/CASH-FEAT-001/evidence/EVID-CASH-001-01.md:1: error [REF-1] referencia a `EVID-CASH-001-01` sin declaración en docs
> FALLA: 3 errores, 0 avisos
> sin cambios: corrija las puertas fallidas
> ```
>
> Regla práctica: la fila de `Evidencias` y su archivo se escriben en el mismo commit.

### 6. Primera release REL-2026.09.1 (2026-09-07)

El responsable de release crea `product/RELEASES.md` con la fila REL-2026.09.1, escribe `released_in` en cada FEATURE y `Liberado en` en cada CHANGELOG:

```text
$ trace promote AUTH-FEAT-001 --to released --date 2026-09-07 --docs $R     → OK
$ trace promote CASH-FEAT-001 --to released --date 2026-09-07 --docs $R     → OK
$ trace promote REPORT-FEAT-001 --to released --date 2026-09-07 --docs $R   → OK
$ trace build --docs $R
actualizado: docs/process/examples/pos-cash-opening/traceability/matrix.md
```

### 7. CHG-001: aprobación de supervisor sobre el umbral (2026-09-14 a 2026-09-29)

La jefatura de caja responde Q-PRD-002 (S/ 500.00, límite exclusivo). Como CASH-FEAT-001 ya está `released`, el comportamiento nuevo entra por un CHG. Sin un CHG abierto que liste el feature, volver a análisis solo produce un aviso:

```text
$ trace next-id CHG --docs $R    →  CHG-001
$ trace new chg "Aprobación de supervisor para montos de apertura sobre umbral" --feature CASH-FEAT-001 --slug aprobacion-supervisor-apertura-sobre-umbral --templates docs/process/templates --date 2026-09-14 --docs $R
creado: docs/process/examples/pos-cash-opening/changes/CHG-001-aprobacion-supervisor-apertura-sobre-umbral.md

$ trace gate CASH-FEAT-001 --to analysis --docs $R
…/CASH-FEAT-001/FEATURE.md:5: aviso [G-TRANS] CASH-FEAT-001: released → analysis requiere un CHG (o BUG) abierto que lo liste
OK: 0 errores, 1 avisos
```

| Paso | Dueño | Resultado en el ejemplo | CHG |
|---|---|---|---|
| 1–2 | Analista | `Motivo` y `requested_by` | proposed |
| 3 | Analista | PRD 0.2.0: REQ-008 (pedido con `trace next-id REQ` → `REQ-008`) | analysis |
| 4–5 | Arquitecto | `Impacto técnico`; `features`: CASH-FEAT-001 `direct`/`minor`, REPORT-FEAT-001 `induced` vía CASH-FEAT-001 `patch` | analysis |
| 6 | QA | Pruebas nuevas TEST-CASH-001-17 a 24, modificadas y regresión | analysis |
| 7 | Solicitante + analista + arquitecto | `Decisión`: aprobado con condiciones (configuración `ApprovalThresholdEnabled`) | approved |
| 8 | Todos | Ambos features `released → analysis`; CASH 1.1.0 (US-CASH-001-03, AC-CASH-001-06 a 09), REPORT 1.0.1 (AC aclarado); CHANGELOG `pendiente`; ambos `→ ready` | approved |
| 9–10 | Desarrolladores | Código y pruebas; features `→ ready-for-qa` | implemented |
| 11–12 | QA | Resultados, EVID-CASH-001-05 a 07, EVID-REPORT-001-04, regresión cruzada; CHANGELOG `aprobado`; features `→ approved` | verified |
| 13 | Release | REL-2026.10.1 | released |

Los IDs nuevos se piden antes de escribir cualquier documento que los cite (el CLI escanea el árbol):

```text
$ trace next-id US --feature CASH-FEAT-001 --docs $R     →  US-CASH-001-03
$ trace next-id AC --feature CASH-FEAT-001 --docs $R     →  AC-CASH-001-06
$ trace next-id BR --feature CASH-FEAT-001 --docs $R     →  BR-CASH-001-06
$ trace next-id ASM --feature CASH-FEAT-001 --docs $R    →  ASM-CASH-001-04
$ trace next-id TEST --feature CASH-FEAT-001 --docs $R   →  TEST-CASH-001-17
```

Con el CHG en `verified`, G-APPROVED-14 comprueba que ambos CHANGELOG tienen una entrada con `Cambio = CHG-001` y `Validación = aprobado`:

```text
$ trace promote CASH-FEAT-001 --to approved --date 2026-09-29 --docs $R     → OK: 0 errores, 0 avisos
$ trace promote REPORT-FEAT-001 --to approved --date 2026-09-29 --docs $R   → OK: 0 errores, 0 avisos
```

### 8. Segunda release REL-2026.10.1 (2026-10-02)

Fila REL-2026.10.1 en `RELEASES.md`, `released_in` en ambos features y en CHG-001 (`status: released`), `Liberado en` en las entradas 1.1.0 y 1.0.1:

```text
$ trace promote CASH-FEAT-001 --to released --date 2026-10-02 --docs $R     → OK
$ trace promote REPORT-FEAT-001 --to released --date 2026-10-02 --docs $R   → OK
$ trace build --docs $R
actualizado: docs/process/examples/pos-cash-opening/traceability/matrix.md
actualizado: docs/process/examples/pos-cash-opening/traceability/open-questions.md
```

## Commits y PR por repositorio

Todos validados con `trace lint-commit` / `trace lint-pr` (exit 0). Hashes y ramas son ilustrativos.

| Repo | Rama | Commits de ejemplo | Título de PR |
|---|---|---|---|
| MarketjoyaBackend | `feat/CASH-FEAT-001-open-cash-session` | `feat(CASH-FEAT-001): abre sesión de caja con índice único e idempotencia`<br>`test(CASH-FEAT-001): cubre TEST-CASH-001-05 carrera entre dos dispositivos` | `feat(CASH-FEAT-001): abrir sesión de caja` |
| MarketjoyaMobile | `fix/BUG-001-double-tap-opening` | `fix(BUG-001): genera ClientMutationId una vez por intento de apertura` | `fix(BUG-001): evitar doble apertura por doble toque` |
| MarketjoyaBackend, MarketjoyaFront, MarketjoyaMobile | `feat/CHG-001-supervisor-approval` | `feat(CHG-001): aprobación de supervisor para aperturas sobre umbral` | `feat(CHG-001): aprobación de supervisor sobre umbral` |
| MarketjoyaBackend | `test/REPORT-FEAT-001-approved-session` | `test(REPORT-FEAT-001): cubre sesión aprobada en TEST-REPORT-001-01` | `test(REPORT-FEAT-001): sesión aprobada en el reporte` |
| Padre (`marketjoya`) | `docs/ADR-001-single-open-session` | `docs(ADR-001): sesión única por caja e idempotencia de apertura` | `docs(ADR-001): sesión única por caja` |
| Padre | `docs/CHG-001-supervisor-approval` | `docs(CHG-001): registra cambio y versiones 1.1.0 y 1.0.1` | `docs(CHG-001): aprobación de supervisor` |
| Padre | `docs/REL-2026.10.1` | `docs(REL-2026.10.1): registrar release` | `docs(REL-2026.10.1): registrar release` |

Lo que el lint rechaza:

```text
$ trace lint-commit "feat: aprobación de supervisor"
error [GIT-2] El commit: el ID (Feature, CHG, BUG o ADR) es obligatorio para feat|fix|test|refactor|perf; p. ej. `feat(PAY-FEAT-001): …`
$ trace lint-pr "Aprobación de supervisor"
error [GIT-1] El título del PR no cumple `<type>(<ID>): <asunto>` con type ∈ feat|fix|test|refactor|perf|docs|chore|build|ci|revert: «Aprobación de supervisor»
```

## Una cadena de trazabilidad completa: AC-CASH-001-07

"Supervisor aprueba la apertura pendiente", de punta a punta:

| Eslabón | Dónde | Valor |
|---|---|---|
| Requisito | [PRD.md](product/PRD.md) → `Requisitos funcionales` | REQ-008 (nace de Q-PRD-002 y CHG-001) |
| Feature | `prd_requirements` de [CASH-FEAT-001](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/FEATURE.md) | CASH-FEAT-001, `spec_version` 1.1.0 |
| Historia | `### US-CASH-001-03 — Aprobar aperturas con monto alto` | US-CASH-001-03 |
| Criterio | `#### AC-CASH-001-07` bajo la historia; regla BR-CASH-001-07 | Given/When/Then con S/ 800.00 |
| Especificación | [TECHNICAL-SPEC](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/TECHNICAL-SPEC.md) → `Backend` (`ApproveCashSessionOpeningCommand`, `CashRegister_ApproveCashSessionOpening`), `Frontend` (pantalla de aprobaciones), `Eventos y mensajería` | `openapi:CashRegister_ApproveCashSessionOpening` en `impact.contracts` |
| Código | `git log --oneline --grep CHG-001` en MarketjoyaBackend y MarketjoyaFront | `feat(CHG-001): aprobación de supervisor para aperturas sobre umbral` (backend `5e8a1f0`, front `3d9e6b2`) |
| Pruebas | [QA-PLAN](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/QA-PLAN.md) → `Casos de prueba` | TEST-CASH-001-19 (`integration`, backend) y TEST-CASH-001-22 (`e2e`, front) |
| Resultado | Columna `Resultado` y fila del 2026-09-29 en `Resultados` | `pasa` |
| Evidencia | `Evidencias` → [EVID-CASH-001-06](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/evidence/EVID-CASH-001-06.md) | Playwright en Chromium y Firefox |
| Release | `released_in`, `Liberado en` de [CHANGELOG](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/CHANGELOG.md), [RELEASES.md](product/RELEASES.md) | REL-2026.10.1 |

La misma cadena, en una fila de la [matriz generada](traceability/matrix.md):

```text
| REQ-001, …, REQ-008 | CASH-FEAT-001 | US-CASH-001-03 | AC-CASH-001-07 | TEST-CASH-001-19<br>TEST-CASH-001-22 | integration<br>e2e | …/ApproveCashSessionOpeningTests.cs<br>e2e/cash-register/approve-cash-session-opening.spec.ts | pasa<br>pasa | EVID-CASH-001-06 | released | 1.1.0 | REL-2026.10.1 |
```

## Qué recibió REPORT-FEAT-001 por culpa de CASH-FEAT-001

| Pregunta | Cómo se responde | Respuesta en el ejemplo |
|---|---|---|
| ¿Qué CHG lo afectaron? | `rg -l 'id: REPORT-FEAT-001' docs/process/examples/pos-cash-opening/changes` | CHG-001 |
| ¿Directo o inducido, y desde dónde? | `features[]` del CHG | `impact: induced`, `via: CASH-FEAT-001`, `bump: patch` |
| ¿Qué cambió exactamente? | Entrada `[1.0.1]` de su [CHANGELOG](features/reports/REPORT-FEAT-001-reporte-de-sesiones-de-caja/CHANGELOG.md) | AC-REPORT-001-01 aclarado (hora de aprobación); BR-REPORT-001-02 aclarada; TEST-REPORT-001-01 modificada; sin código de producción |
| ¿Por qué? | [Especificación de CASH](features/cash-register/CASH-FEAT-001-abrir-sesion-de-caja/TECHNICAL-SPEC.md) → `Eventos y mensajería` | `CashSessionOpenedIntegrationEvent` ahora también se publica al aprobar; `impact.features_affected: [REPORT-FEAT-001]` |
| ¿Qué regresión cruzada se corrió? | Tabla `Regresión` de ambos QA-PLAN | En CASH: TEST-REPORT-001-01 y 03; en REPORT: TEST-CASH-001-08 y 19; todas `pasa` |
| ¿En qué release llegó? | `Liberado en`, `released_in` del CHG, fila de `RELEASES.md` | REL-2026.10.1 |

## Verificación final

```text
$ trace check --docs docs/process/examples/pos-cash-opening
OK: 0 errores, 0 avisos
$ trace build --docs docs/process/examples/pos-cash-opening --check
OK: traceability/ al día
$ trace gate CASH-FEAT-001 --to released --docs docs/process/examples/pos-cash-opening
gate CASH-FEAT-001 → released
OK: 0 errores, 0 avisos
```

Las dos preguntas no críticas que siguen abiertas (Q-CASH-001-02, Q-REPORT-001-01) aparecen en [open-questions.md](traceability/open-questions.md): no bloquean ninguna puerta.

Prueba de que las puertas detectan problemas, sobre una **copia** del ejemplo (no sobre este árbol) en la que se marcó TEST-CASH-001-22 como `falla`, se borró `evidence/EVID-CASH-001-06.md` y se vació `Liberado en` de la entrada 1.1.0:

```text
$ trace gate CASH-FEAT-001 --to released --docs <copia>
gate CASH-FEAT-001 → released
<copia>/…/CASH-FEAT-001/CHANGELOG.md:21: error [G-RELEASED-15] CASH-FEAT-001: `released_in` REL-2026.10.1 no figura en `Liberado en` de la entrada vigente del CHANGELOG
<copia>/…/CASH-FEAT-001/QA-PLAN.md:103: error [G-APPROVED-9] CASH-FEAT-001: TEST-CASH-001-22 tiene Resultado `falla`
<copia>/…/CASH-FEAT-001/QA-PLAN.md:162: error [G-APPROVED-11] CASH-FEAT-001: EVID-CASH-001-06: no existe el archivo `evidence/EVID-CASH-001-06.md`
FALLA: 3 errores, 0 avisos
$ trace build --check --docs <copia>
desactualizado: <copia>/traceability/matrix.md
FALLA: ejecute `tools/traceability/trace build` y commitee el resultado
```

## Fricciones observadas al construir el ejemplo

| Situación | Efecto | Cómo se resolvió aquí |
|---|---|---|
| `trace new` busca plantillas en `<docs>/process/templates` | Falla dentro de un ejemplo | `--templates docs/process/templates` |
| Archivo `evidence/EVID-…md` creado antes de su fila | REF-1 bloquea cualquier `promote` del feature | Fila y archivo en el mismo cambio |
| `target_release` apunta a un REL que aún no está en `RELEASES.md` | REF-1 una vez que el archivo existe | No se usó `target_release` (no hubo dependencias en validación); la alternativa es registrar la fila del REL antes de liberar |
| Un CHG solo bloquea `released → analysis` con aviso, no con error | Se puede reabrir un feature sin CHG si se ignoran los avisos | Se abrió CHG-001 antes de promover |
| Estado del CHG se edita a mano | No hay `trace promote` para CHG | Edición del frontmatter en cada paso, registrada en la tabla de pasos de `Decisión` |

## Siguiente paso

Para un feature real, empieza por el [camino rápido del proceso](../../README.md) y el [esquema](../../traceability-schema.md); la gestión de cambios está en [05-gestion-de-cambios.md](../../05-gestion-de-cambios.md) y la referencia del CLI en [tools/traceability/README.md](../../../../tools/traceability/README.md).
