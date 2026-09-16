# Esquema de trazabilidad (contrato para humanos, agentes y tooling)

Fuente única de formatos del proceso de features. Plantillas (`docs/process/templates/`), skills de rol y `tools/traceability/` deben cumplir este archivo. Si hay contradicción, manda este archivo.

## 1. Estructura de carpetas

```text
docs/
├── process/                      metodología, plantillas, ejemplos (este archivo)
│   ├── templates/                FEATURE.md, TECHNICAL-SPEC.md, QA-PLAN.md, CHANGELOG.md, CHG.md, BUG.md, ADR.md, PRD.md, FUNCTIONAL-ANALYSIS.md, VIEWS.md
│   └── examples/<nombre>/        árbol docs completo de ejemplo (misma forma que docs/)
├── product/
│   ├── modules.yaml              registro de módulos (código → slug → nombre)
│   ├── PRD.md                    requisitos REQ-### y preguntas Q-PRD-###
│   ├── RELEASES.md               registro REL-YYYY.MM.N
│   ├── FUNCTIONAL-ANALYSIS.md    vistas, flujos, acciones, estados y reglas visibles
│   └── VIEWS.md                  inventario VIEW-### / FLOW-### con enlace al maquetado
├── features/<module-slug>/<FEATURE-ID>-<slug>/
│   ├── FEATURE.md
│   ├── TECHNICAL-SPEC.md
│   ├── QA-PLAN.md
│   ├── CHANGELOG.md
│   └── evidence/                 EVID-*.md|png|json|txt (evidencia QA)
├── changes/CHG-###-<slug>.md
├── bugs/BUG-###-<slug>.md
├── adr/ADR-###-<slug>.md
└── traceability/                 GENERADO por `trace build`; no editar a mano
    ├── matrix.md
    ├── dependency-map.md
    └── open-questions.md
tools/traceability/               CLI `trace` (Python 3 + PyYAML)
```

`<slug>`: kebab-case ASCII, `^[a-z0-9]+(-[a-z0-9]+)*$`.

## 2. Identificadores

| Tipo | Formato | Regex | Ámbito | Ejemplo |
|---|---|---|---|---|
| Módulo | 2–8 mayúsculas | `^[A-Z]{2,8}$` | `modules.yaml` | `PAY` |
| Requisito PRD | `REQ-###` | `^REQ-\d{3}$` | global | `REQ-012` |
| Vista / flujo | `VIEW-###` / `FLOW-###` | `^(VIEW\|FLOW)-\d{3}$` | global | `VIEW-004` |
| Feature | `<MOD>-FEAT-###` | `^[A-Z]{2,8}-FEAT-\d{3}$` | por módulo | `PAY-FEAT-001` |
| Historia | `US-<MOD>-###-##` | `^US-[A-Z]{2,8}-\d{3}-\d{2}$` | por feature | `US-PAY-001-01` |
| Criterio | `AC-<MOD>-###-##` | `^AC-[A-Z]{2,8}-\d{3}-\d{2}$` | por feature | `AC-PAY-001-03` |
| Prueba | `TEST-<MOD>-###-##` | `^TEST-[A-Z]{2,8}-\d{3}-\d{2}$` | por feature | `TEST-PAY-001-07` |
| Pregunta | `Q-<MOD>-###-##` | `^Q-[A-Z]{2,8}-\d{3}-\d{2}$` | por feature | `Q-PAY-001-02` |
| Pregunta de producto | `Q-PRD-###` | `^Q-PRD-\d{3}$` | global (PRD) | `Q-PRD-004` |
| Suposición | `ASM-<MOD>-###-##` | `^ASM-[A-Z]{2,8}-\d{3}-\d{2}$` | por feature | `ASM-PAY-001-01` |
| Evidencia | `EVID-<MOD>-###-##` | `^EVID-[A-Z]{2,8}-\d{3}-\d{2}$` | por feature | `EVID-PAY-001-01` |
| Regla de negocio | `BR-<MOD>-###-##` | `^BR-[A-Z]{2,8}-\d{3}-\d{2}$` | por feature | `BR-PAY-001-02` |
| Cambio | `CHG-###` | `^CHG-\d{3}$` | global | `CHG-004` |
| Incidencia | `BUG-###` | `^BUG-\d{3}$` | global | `BUG-017` |
| Decisión | `ADR-###` | `^ADR-\d{3}$` | global | `ADR-002` |
| Release | `REL-YYYY.MM.N` | `^REL-\d{4}\.\d{2}\.\d+$` | global | `REL-2026.10.1` |

Reglas:
- Los IDs con `<MOD>-###` embebido pertenecen al feature `<MOD>-FEAT-###`; el tooling deriva el feature del ID.
- Un ID nunca se reutiliza ni se renumera. Deprecar = estado, no borrado.
- Siguiente número: `trace next-id <TIPO> [--feature <ID>]`. Duplicados = error de CI.
- Colisión de IDs globales entre ramas (`REQ`, `CHG`, `BUG`, `ADR`, `<MOD>-FEAT`): **renumera quien integra último**, antes del merge, actualizando todas sus referencias. Los IDs con ámbito de feature no colisionan entre features.
- `<MOD>` debe existir en `modules.yaml`.

## 3. Declaración vs referencia

- **Declaración**, una sola vez en todo el árbol validado:
  - Frontmatter `id` de FEATURE (feature), CHG, BUG y ADR.
  - Inicio de encabezado: `### US-…`, `#### AC-…`.
  - Primera celda de fila en las **tablas designadas**:

    | Archivo | Tabla (H2) | Declara |
    |---|---|---|
    | FEATURE.md | `Reglas de negocio` / `Suposiciones` / `Preguntas pendientes` | `BR` / `ASM` / `Q` |
    | QA-PLAN.md | `Casos de prueba` / `Evidencias` | `TEST` / `EVID` |
    | product/PRD.md | `Requisitos funcionales`, `Requisitos no funcionales` | `REQ` |
    | product/PRD.md | `Preguntas abiertas` | `Q-PRD-###` |
    | product/VIEWS.md | `Vistas` / `Flujos` | `VIEW` / `FLOW` |
    | product/RELEASES.md | `Releases` | `REL` |

- **Referencia**: cualquier otra aparición. Toda referencia debe resolver a una declaración. `REQ`/`VIEW`/`FLOW`/`REL` solo se exigen si el archivo que los declara existe.
- Los **comentarios HTML** (`<!-- … -->`) se eliminan antes de analizar: ahí van ejemplos y guías de plantilla.
- **Alcance de `trace check`**: `product/`, `features/`, `changes/`, `bugs/`, `adr/` bajo `--docs`. Nunca `process/`; los ejemplos se validan apuntando `--docs` a su raíz (`docs/process/examples/<nombre>`).
- En código y tests el ID aparece como token literal (§8).

Formatos de las tablas de producto (columnas en este orden):

- PRD `Requisitos funcionales`: `| ID | Requisito | Prioridad | Origen | Vistas | Módulo | Estado |`
- PRD `Requisitos no funcionales`: `| ID | Requisito | Categoría | Métrica objetivo | Origen | Estado |`
- PRD `Preguntas abiertas`: `| ID | Pregunta | Crítica | Estado | Respuesta |`
- VIEWS `Vistas`: `| ID | Nombre | Plataforma | Tipo | Maquetado | Roles | Módulo | Estado |`
- VIEWS `Flujos`: `| ID | Nombre | Actor | Vistas en orden | Maquetado | Estado |`
- RELEASES `Releases`: `| ID | Fecha | Repos y tags | Features | Notas |`
- FUNCTIONAL-ANALYSIS: una H2 `## Vista: <nombre> (VIEW-###)` por vista (referencia, no declaración).

## 4. Frontmatter y secciones obligatorias

Todas las fechas `YYYY-MM-DD`. Versiones SemVer `^\d+\.\d+\.\d+$`.

### 4.1 `modules.yaml`

```yaml
modules:
  - code: PAY          # regex §2
    slug: payments     # carpeta en docs/features/
    name: Pagos
    repos: [backend, front, mobile]   # subconjunto de backend|front|mobile
```

### 4.2 `FEATURE.md`

```yaml
---
id: PAY-FEAT-001
title: Registrar pago
module: PAY
status: draft            # §5
spec_version: 1.0.0
owners: { analyst: "", architect: "", developer: "", qa: "" }
prd_requirements: [REQ-012]
views: [VIEW-004]
depends_on: []           # features requeridos para funcionar
related: []              # features que consumen o son afectados (sin dependencia dura)
repos: [backend, front]
blocked_reason: ""       # obligatorio no vacío si status ∈ {blocked, rejected}
blocked_from: ""         # estado previo; obligatorio si status = blocked (a él se vuelve)
target_release: ""       # REL-… objetivo; opcional hasta in-validation
released_in: ""          # REL-… obligatorio si status ∈ {released, deprecated}
updated: 2026-09-15
---
```

Encabezados H2 obligatorios, en este orden: `Objetivo`, `Problema`, `Alcance`, `Fuera de alcance`, `Actores, roles y permisos`, `Precondiciones`, `Flujo principal`, `Flujos alternativos`, `Estados`, `Reglas de negocio`, `Validaciones`, `Mensajes de error`, `Historias de usuario`, `Dependencias y features relacionados`, `Vistas de referencia`, `Suposiciones`, `Preguntas pendientes`.

- Historia: `### US-…-## — <título>` seguido de la línea `Como …, quiero …, para ….`
- Criterio: `#### AC-…-## — <título>` bajo su historia, con líneas que empiezan por `Given`, `When`, `Then` (en ese orden; `And` permitido).
- Reglas de negocio: tabla `| ID | Regla | Origen |` con IDs `BR-<MOD>-###-##` (regex `^BR-[A-Z]{2,8}-\d{3}-\d{2}$`; Origen = `REQ-###`, `ASM-…` o `CHG-###`).
- Suposiciones: tabla `| ID | Suposición | Validar con | Estado |` (Estado ∈ `pendiente|confirmada|descartada`).
- Preguntas: tabla `| ID | Pregunta | Crítica | Estado | Respuesta |` (Crítica ∈ `si|no`; Estado ∈ `abierta|resuelta`).
- **Todos los valores enumerados de este esquema son ASCII** (sin tildes) para que el tooling no dependa de normalización Unicode; el texto libre sí lleva tildes.

### 4.3 `TECHNICAL-SPEC.md`

```yaml
---
feature: PAY-FEAT-001
spec_version: 1.0.0        # igual a FEATURE.md
status: draft              # draft | reviewed | approved
adrs: []                   # ADR-###
impact:
  modules_direct: [PAY]
  modules_indirect: []
  features_affected: []    # features cuya doc/pruebas cambian
  regression_tests: []     # TEST-… de otros features a re-ejecutar
  contracts: []            # p. ej. "openapi:Payments_RegisterPayment", "event:PaymentRegisteredIntegrationEvent"
  migrations: false
updated: 2026-09-15
---
```

H2 obligatorios: `Resumen`, `Arquitectura actual involucrada`, `Reutilización`, `Componentes nuevos`, `Backend`, `Frontend`, `Mobile`, `Base de datos y migraciones`, `Contratos`, `Eventos y mensajería`, `Integraciones externas`, `Seguridad y autorización`, `Observabilidad`, `Errores`, `Concurrencia y transacciones`, `Compatibilidad y datos existentes`, `Despliegue y reversión`, `Riesgos`, `Análisis de impacto`, `Archivos a crear o modificar`, `Orden de implementación`. Sección que no aplica: contenido `No aplica — <motivo>`.

### 4.4 `QA-PLAN.md`

```yaml
---
feature: PAY-FEAT-001
spec_version: 1.0.0
status: draft              # draft | ready | executing | passed | failed
last_run: ""               # fecha de la última ejecución registrada
updated: 2026-09-15
---
```

QA escribe el diseño del plan (casos, criterios cubiertos, niveles, datos, roles) **durante `analysis`**, en paralelo a la TECHNICAL-SPEC, y lo deja en `status: ready`; es condición de G-READY. La ruta de automatización la completa desarrollo; resultados y evidencias, QA.

H2 obligatorios: `Objetivo`, `Alcance`, `Fuera de alcance`, `Criterios de entrada`, `Criterios de salida`, `Entornos`, `Datos de prueba`, `Matriz de roles y permisos`, `Casos de prueba`, `Regresión`, `No funcionales`, `Evidencias`, `Resultados`.

Tabla `Casos de prueba` (declara TEST):

`| ID | Criterios | Tipo | Nivel | Repo | Automatización | Resultado |`

- Criterios: lista de `AC-…` separada por coma (o `—` si es regresión/no funcional sin AC).
- Tipo ∈ `positivo|negativo|limite|error|concurrencia|permisos|seguridad|rendimiento|accesibilidad|compatibilidad|regresion|recuperacion|migracion`.
- Nivel ∈ `unit|domain|integration|database|contract|cross-module|messaging|component|e2e|visual|migration`.
- Repo ∈ `backend|front|mobile|manual`.
- Automatización: ruta relativa al repo del test (p. ej. `tests/Marketjoya.Api.E2ETests/Payments/RegisterPaymentTests.cs`) o `manual: <motivo>`.
- Resultado ∈ `pendiente|pasa|falla|bloqueado`.

Tabla `Regresión`: `| TEST | Feature | Motivo | Resultado |`. Tabla `Evidencias` (declara EVID): `| ID | TEST | Archivo o enlace | Fecha |` (archivo relativo a `evidence/` o URL de CI).

### 4.5 `CHANGELOG.md`

Sin frontmatter. Entradas de la más nueva a la más vieja:

```markdown
## [1.1.0] — 2026-09-20

| Campo | Valor |
|---|---|
| Cambio | CHG-004 |
| Tipo | minor |
| Descripción | … |
| Motivo | … |
| Responsable | nombre o agente/rol |
| Requisitos | REQ-012 |
| Criterios afectados | AC-PAY-001-03 (modificado), AC-PAY-001-06 (nuevo) |
| Archivos técnicos | backend: src/…; front: src/… |
| Pruebas | TEST-PAY-001-09 (nueva) |
| Features relacionados | NOTIF-FEAT-002 (inducido) |
| Riesgos | … |
| Validación | pendiente \| aprobado \| rechazado |
| Liberado en | REL-2026.10.1 \| — |
```

Encabezado: `## [X.Y.Z] — YYYY-MM-DD`. La primera versión (`1.0.0`) puede tener `Cambio: —`. Tipo ∈ `major|minor|patch|initial`.

### 4.6 `CHG-###`

```yaml
---
id: CHG-004
title: Pago parcial
status: proposed   # proposed | analysis | approved | implemented | verified | released | rejected
requested_by: ""
date: 2026-09-18
features:
  - id: PAY-FEAT-001
    impact: direct        # direct | induced
    bump: minor           # major | minor | patch
  - id: NOTIF-FEAT-002
    impact: induced
    via: PAY-FEAT-001
    bump: patch
requirements: [REQ-012]
released_in: ""
---
```

H2: `Motivo`, `Requisitos que cambian`, `Impacto técnico`, `Features afectados`, `Plan de pruebas y regresión`, `Evidencias`, `Decisión`.

Transiciones de CHG (validadas por `trace promote CHG-###`):

| Desde | Hacia | Condición |
|---|---|---|
| `proposed` | `analysis`, `rejected` | `Motivo` no vacío |
| `analysis` | `approved` | `Impacto técnico` y `Features afectados` no vacíos; ≥1 feature `direct` |
| `analysis` | `rejected` | `--reason` obligatorio |
| `approved` | `implemented` | cada feature listado en estado ≥ `in-progress` |
| `implemented` | `verified` | cada feature listado en `approved` o `released` y con entrada de CHANGELOG que cita el CHG |
| `verified` | `released` | `released_in` definido (y existe en RELEASES.md si el archivo existe) |
| `rejected` | `proposed` | `--reason` obligatorio |

### 4.7 `BUG-###`

```yaml
---
id: BUG-017
title: Pago duplicado con doble clic
feature: PAY-FEAT-001  # puede quedar vacío solo en status open (p. ej. bug de producción sin triage); obligatorio desde in-progress
severity: major        # critical | major | minor | trivial
status: open           # open | in-progress | fixed | verified | closed | wontfix
source: qa             # qa | ci | production | review
found_in: ""           # versión/commit/release
tests: [TEST-PAY-001-07]
criteria: [AC-PAY-001-02]
date: 2026-09-19
---
```

H2: `Descripción`, `Pasos para reproducir`, `Resultado esperado`, `Resultado obtenido`, `Fuente de verdad` (PRD / FEATURE / comportamiento), `Evidencia`, `Resolución`.

Transiciones de BUG (validadas por `trace promote BUG-###`):

| Desde | Hacia | Condición |
|---|---|---|
| `open` | `in-progress`, `wontfix` | `feature` definido para `in-progress`; `wontfix` exige `--reason` |
| `in-progress` | `fixed` | `Resolución` no vacía |
| `fixed` | `verified`, `in-progress` | `verified`: `tests` no vacío y todos con `Resultado = pasa` en el QA-PLAN del feature |
| `verified` | `closed` | — |
| `wontfix`, `closed` | `open` | `--reason` obligatorio (reapertura) |

### 4.8 `ADR-###`

```yaml
---
id: ADR-002
title: Idempotencia de pagos por client_mutation_id
status: accepted     # proposed | accepted | superseded | deprecated
date: 2026-09-16
features: [PAY-FEAT-001]
supersedes: ""
superseded_by: ""
---
```

H2: `Contexto`, `Decisión`, `Alternativas consideradas`, `Consecuencias`, `Justificación de lo nuevo`.

`Justificación de lo nuevo` es obligatoria cuando el ADR introduce servicio, patrón, cola, evento o mecanismo de comunicación, y responde en este orden (una H3 por pregunta, `No aplica — <motivo>` permitido):

1. ¿Qué problema resuelve?
2. ¿Por qué no puede reutilizarse la arquitectura existente? (qué se buscó y dónde)
3. ¿Qué módulos participan?
4. ¿Qué contratos utiliza? (OpenAPI operationId, evento de integración, tabla, puerto)
5. ¿Qué errores pueden producirse? (con `code` del contrato de errores)
6. ¿Cómo se recupera de esos errores? (reintentos, idempotencia, DLQ, compensación)
7. ¿Cómo se monitorea? (trazas, métricas, logs, alertas)
8. ¿Cómo se prueba? (TEST y niveles)
9. ¿Qué impacto tiene en otros features? (IDs)

## 5. Estados del feature

| Clave | Etiqueta | Siguiente permitido |
|---|---|---|
| `draft` | Borrador | `analysis` |
| `analysis` | En análisis | `ready`, `blocked`, `rejected` |
| `ready` | Listo para desarrollo | `in-progress`, `blocked` |
| `in-progress` | En desarrollo | `ready-for-qa`, `blocked` |
| `ready-for-qa` | Listo para QA | `in-validation`, `in-progress` |
| `in-validation` | En validación | `approved`, `blocked`, `in-progress` |
| `blocked` | Bloqueado | el estado de `blocked_from` |
| `rejected` | Rechazado | `analysis` |
| `approved` | Aprobado | `released`, `in-progress` (por CHG/BUG) |
| `released` | Liberado | `analysis` (por CHG), `deprecated` |
| `deprecated` | Deprecado | — |

Reglas de reapertura (error **G-TRANS**, no aviso):
- `approved → in-progress` exige un CHG en `approved` que liste el feature, o un BUG del feature en `open`/`in-progress`.
- `released → analysis` exige un CHG en `proposed`, `analysis` o `approved` que liste el feature.
- Cualquier transición fuera de esta tabla es error G-TRANS.

## 6. Puertas verificables (`trace gate <ID> --to <estado>`; CI valida el estado actual de cada feature)

Un feature en estado S debe cumplir las puertas de S y de todos los estados anteriores del camino feliz.

**G-READY** (`ready` y posteriores, salvo `blocked`/`rejected`):
1. Los 4 archivos existen con frontmatter válido; `spec_version` idéntico en FEATURE, TECHNICAL-SPEC, QA-PLAN y la primera entrada de CHANGELOG.
2. ≥1 historia; cada historia con ≥1 AC; cada AC con Given, When y Then.
3. Ninguna pregunta con `Crítica = si` y `Estado = abierta`.
4. `TECHNICAL-SPEC.status ∈ {reviewed, approved}`; ningún H2 obligatorio vacío; `impact.modules_direct` no vacío.
5. Cada AC referenciado por ≥1 TEST en `Casos de prueba`, y `QA-PLAN.status ≠ draft`.
6. Todo `depends_on`/`related`/`impact.features_affected` existe; todo `adrs` existe y no está `superseded`/`deprecated`.
7. Todo `REQ`/`VIEW` referenciado existe en `PRD.md`/`VIEWS.md` (si esos archivos existen).

**G-QA** (`ready-for-qa` y posteriores): 8. Todo TEST no manual tiene ruta de automatización (la completa desarrollo).

**G-APPROVED** (`approved`, `released`):
9. Todo TEST con `Resultado = pasa`; toda fila de `Regresión` con `pasa`.
10. `QA-PLAN.status = passed` y `last_run` definido.
11. ≥1 EVID por TEST automatizado de nivel `e2e` y por cada TEST `manual`; archivos locales existen.
12. Ningún BUG del feature con `severity ∈ {critical, major}` y `status ∈ {open, in-progress, fixed}`.
13. Cada `depends_on` en `approved` o `released`; o en `ready-for-qa`/`in-validation` con `target_release` no vacío e igual al del feature.
14. Todo CHG que liste el feature (direct o induced) en estado ≥ `implemented` tiene entrada en su CHANGELOG que lo cite; la entrada más reciente con `Validación = aprobado`.

**G-RELEASED**: 15. `released_in` definido y presente en la entrada vigente de CHANGELOG (`Liberado en`).

**G-BLOCKED/REJECTED**: `blocked_reason` no vacío; en `blocked`, `blocked_from` es un estado válido distinto de `blocked`. Salir de `blocked` exige las puertas del estado destino.

Con `--code <ruta-repo>...`: 16. Toda ruta de automatización existe en el repo indicado y contiene el token del TEST (§8).

## 7. Artefactos generados (`trace build`)

- `matrix.md`: una fila por AC: `REQ | Feature | US | AC | TEST | Nivel | Automatización | Resultado | EVID | Estado feature | Versión | Release`.
- `dependency-map.md`: diagrama Mermaid (`depends_on` línea sólida, `related` punteada) + tabla inversa "quién depende de mí".
- `open-questions.md`: preguntas abiertas y suposiciones pendientes, críticas primero.

CI falla si `trace build` produce diferencias con lo commiteado.

## 8. Token de ID en código y tests

El token literal `TEST-<MOD>-###-##` debe aparecer en el nombre o metadato del test:

| Stack | Forma |
|---|---|
| .NET xUnit | `[Trait("TestId", "TEST-PAY-001-01")]` y/o `DisplayName` |
| Vitest / Playwright | `it('TEST-PAY-001-01 registra pago total', …)` |
| Kotlin JUnit / Compose | nombre con backticks `` `TEST-PAY-001-01 registra pago total` `` o comentario `// TEST-PAY-001-01` en la línea anterior |
| Maestro | `# TEST-PAY-001-01` en la cabecera del flujo |

Código de producción: sin tokens obligatorios; la trazabilidad a código va por commits/PR (§9) y por `Archivos a crear o modificar`.

## 9. Git

- Commit: `<type>(<ID>): <asunto>`; `type` ∈ `feat|fix|test|refactor|perf|docs|chore|build|ci|revert`; `<ID>` ∈ Feature, CHG, BUG o ADR. Obligatorio para `feat|fix|test|refactor|perf`; opcional para el resto. Regex: `^(feat|fix|test|refactor|perf|docs|chore|build|ci|revert)(\((([A-Z]{2,8}-FEAT-\d{3})|CHG-\d{3}|BUG-\d{3}|ADR-\d{3}|REL-\d{4}\.\d{2}\.\d+|[a-z0-9-]+)\))?!?: .+$` (el scope `REL-…` se usa en commits de release, p. ej. `docs(REL-2026.10.1): registrar release`), con la regla de obligatoriedad aplicada aparte.
- Título de PR: mismo formato; cuerpo con la plantilla de PR (IDs, puertas, evidencias).
- Rama: `<type>/<ID>-<slug>` (p. ej. `feat/PAY-FEAT-001-register-payment`).
- Repos separados: la documentación vive en el repo padre y el código en `MarketjoyaBackend`, `MarketjoyaFront` y `MarketjoyaMobile`.
  - **Repo padre**: un PR que toca `docs/features/<…>/<ID>-*` debe llevar ese ID en el título o en algún commit del PR, o un `CHG-###`/`BUG-###` (en título o commits) que liste ese feature. Lo valida `trace lint-pr-scope` en CI (regla **GIT-3**). Cambios solo en `docs/traceability/` generado no requieren ID.
  - **Repos de código**: commits y título de PR con el ID (misma regex); el cuerpo del PR enlaza la ruta del feature en el repo padre. La trazabilidad código → feature se reconstruye con `git log --grep <ID>` en cada repo y con `trace check --code`.

## 10. CLI `trace` (contrato mínimo)

`trace gate` solo evalúa y no modifica archivos. `trace promote` evalúa y, si pasa, edita el frontmatter.

| Comando | Efecto |
|---|---|
| `trace promote <FEATURE-ID> --to <estado> [--reason "<texto>"] [--docs raíz]` | Ejecuta `gate`; si pasa, actualiza `status`, `updated` y, al bloquear/rechazar, `blocked_reason` (obligatorio `--reason`) y `blocked_from`; al salir de `blocked`, limpia ambos |
| `trace promote CHG-###\|BUG-### --to <estado> [--reason "<texto>"]` | Valida la transición y sus condiciones (§4.6, §4.7); si pasa, actualiza `status` |
| `trace lint-pr-scope --title "<título>" --files <archivo-con-rutas> [--commits <archivo-con-mensajes>] [--docs raíz]` | Regla GIT-3 (§9) para el repo padre |
| `trace new feature <MOD> "<título>" [--slug s]` | Crea carpeta y 4 archivos desde plantillas con el siguiente número |
| `trace new chg\|bug\|adr "<título>" [--feature ID]` | Crea el documento con el siguiente número |
| `trace next-id <TIPO> [--feature ID]` | Imprime el siguiente ID libre |
| `trace check [--docs <raíz>] [--code backend=<ruta> front=<ruta> mobile=<ruta>]` | Valida §2–§6 (+§8 con `--code`); exit 1 con errores |
| `trace gate <FEATURE-ID> --to <estado>` | Evalúa si el feature puede pasar al estado; lista puertas fallidas |
| `trace build [--docs <raíz>]` | Regenera `traceability/` |
| `trace lint-commit "<mensaje>"` / `trace lint-pr "<título>"` | Valida §9 |

Opción global `--date YYYY-MM-DD`: fija la fecha que escriben `new` y `promote` (simular líneas de tiempo en ejemplos y tests); en uso real se omite.

Raíz por defecto `--docs docs`. Placeholders de plantilla: `{{ID}}` (ID del documento; en las 4 plantillas de feature es el feature), `{{TITLE}}`, `{{MODULE}}`, `{{MODULE_SLUG}}`, `{{SLUG}}`, `{{DATE}}`, `{{FEATURE}}` (feature relacionado en CHG/BUG/ADR; vacío si no hay), `{{NUM}}` (número de 3 dígitos del feature, para escribir `US-{{MODULE}}-{{NUM}}-01`).
