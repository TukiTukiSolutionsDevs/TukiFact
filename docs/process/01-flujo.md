# Flujo de 14 etapas: del maquetado a producción

Cada etapa tiene **una entrada, un artefacto de salida con ruta fija, un rol dueño y un criterio de salida verificable**. Ninguna etapa se da por terminada si su artefacto no existe en el repositorio.

## Resumen

| # | Etapa | Dueño | Artefacto de salida | Estado / puerta al salir |
|---|---|---|---|---|
| 1 | Maquetado | Analista | `docs/product/VIEWS.md` (enlaces) | — |
| 2 | Análisis funcional | Analista | `docs/product/FUNCTIONAL-ANALYSIS.md` | — |
| 3 | PRD | Analista | `docs/product/PRD.md` (`REQ-###`) | — |
| 4 | Módulos | Arquitecto | `docs/product/modules.yaml` | — |
| 5 | Features | Analista + Arquitecto | carpeta `docs/features/<mod>/<ID>-<slug>/` | `draft` |
| 6 | Historias y criterios | Analista | `FEATURE.md` (US, AC, BR) | `analysis` |
| 7 | Especificación técnica y diseño de QA | Arquitecto + QA | `TECHNICAL-SPEC.md` (`reviewed`), `QA-PLAN.md` (`ready`) | — |
| 8 | Análisis de impacto | Arquitecto | `TECHNICAL-SPEC.md` → `impact`, ADR | G-READY → `ready` |
| 9 | Implementación | Desarrollador | código en repos hijos, PR con ID | `in-progress` |
| 10 | Pruebas automatizadas | Desarrollador | tests con token `TEST-…` | G-QA → `ready-for-qa` |
| 11 | Validación QA | QA | `QA-PLAN.md` resultados, `evidence/` | `in-validation` |
| 12 | Regresión | QA | tabla `Regresión` en `QA-PLAN.md` | — |
| 13 | Aprobación y release | QA + responsable de release | `CHANGELOG.md`, `released_in` | G-APPROVED → `approved`; G-RELEASED → `released` |
| 14 | Seguimiento de cambios | Analista + Arquitecto | `docs/changes/CHG-###`, `docs/bugs/BUG-###` | vuelve a `analysis` o `in-progress` |

## Detalle por etapa

### 1. Maquetado

- **Entrada:** pantallas (Figma, imágenes, prototipos).
- **Salida:** cada pantalla o modal registrado como `VIEW-###` y cada recorrido como `FLOW-###` en `VIEWS.md`, con enlace al maquetado.
- **Salida lista cuando:** ninguna pantalla del alcance queda sin ID.

> El maquetado es **fuente de descubrimiento, no de verdad**. Muestra lo visible; no muestra reglas, permisos ni procesos. Usa el checklist de la etapa 2.

### 2. Análisis funcional

- **Entrada:** `VIEWS.md`, conversación con negocio.
- **Salida:** `FUNCTIONAL-ANALYSIS.md`, una sección por vista (propósito, campos, acciones, estados, reglas visibles, errores, permisos) separando **inferido** de **por confirmar**.
- **Salida lista cuando:** el checklist de descubrimiento está recorrido para cada vista.

#### Checklist de descubrimiento (lo que el maquetado no dice)

- [ ] Reglas de negocio (cálculos, límites, redondeos, vigencias).
- [ ] Roles y permisos (quién ve, quién ejecuta, quién aprueba).
- [ ] Validaciones por campo y entre campos.
- [ ] Estados de las entidades y transiciones permitidas.
- [ ] Casos alternativos y de error (sin stock, sin conexión, sesión vencida, duplicados).
- [ ] Procesos en segundo plano (sincronización offline, cierres, reintentos).
- [ ] Comunicación entre módulos (qué dato de otro módulo se lee o se modifica).
- [ ] Integraciones externas: SUNAT, OCR, GPS y otras.
- [ ] Eventos y notificaciones (a quién, cuándo, por qué canal).
- [ ] Seguridad (datos sensibles, auditoría de accesos, secretos fuera del cliente).
- [ ] Rendimiento (volumen, tiempos esperados en POS, operación offline).
- [ ] Auditoría (quién hizo qué y cuándo; qué debe quedar registrado).
- [ ] Migración de datos existentes.

**Lo que no se puede confirmar se registra**, nunca se inventa:

| Situación | Registro | Efecto |
|---|---|---|
| Falta información y bloquea el diseño | `Q-…` con `Crítica = si` | Bloquea G-READY hasta resolverse |
| Falta información pero se puede avanzar | `Q-…` con `Crítica = no` | No bloquea |
| Se asume un comportamiento razonable | `ASM-…` con `Validar con` | La regla que dependa de ella cita `ASM-…` como origen |

A nivel producto (antes de existir el feature), las dudas se declaran como `Q-PRD-###` en `Preguntas abiertas` de `PRD.md` y se trasladan como `Q-…` del feature al crearlo.

### 3. PRD

- **Entrada:** análisis funcional.
- **Salida:** `PRD.md` con requisitos `REQ-###` (prioridad, origen, vistas), requisitos no funcionales, catálogo de reglas, integraciones y glosario.
- **Salida lista cuando:** cada vista del alcance está cubierta por al menos un `REQ`.

### 4. Módulos

- **Entrada:** PRD.
- **Salida:** `modules.yaml` con código, slug, nombre y repos.
- **Salida lista cuando:** cada `REQ` tiene un módulo candidato. Coherencia obligatoria con los módulos del backend (un schema Postgres por módulo, ver `backend-architecture`).

### 5. Features

- **Entrada:** PRD y módulos.
- **Salida:** `trace new feature <MOD> "<título>"` → carpeta con 4 archivos en `draft`.
- **Salida lista cuando:** el feature declara `prd_requirements`, `views` y `repos`. Un feature = una capacidad entregable de valor, verificable de punta a punta.

### 6. Historias y criterios

- **Entrada:** feature en `draft`.
- **Salida:** `FEATURE.md` completo: `US-…` con `Como/quiero/para`, `AC-…` Given/When/Then, reglas `BR-…`, validaciones, mensajes de error (con `code` del [contrato de errores](../../.agents/skills/backend-architecture/references/api-error-contract.md)).
- **Salida lista cuando:** cada historia tiene AC; preguntas críticas resueltas. `trace promote <ID> --to analysis`.

### 7. Especificación técnica y diseño de QA

Arquitecto y QA trabajan **en paralelo durante `analysis`**, ambos a partir de `FEATURE.md`.

- **Entrada:** `FEATURE.md` estable.
- **Salida del arquitecto:** `TECHNICAL-SPEC.md` con los 21 H2 del esquema; lo que no aplica dice `No aplica — <motivo>`.
- **Salida de QA:** `QA-PLAN.md` diseñado (casos con criterios, tipo, nivel y repo; datos; matriz de roles), `status: ready`. La columna `Automatización` queda vacía para el desarrollador.
- **Salida lista cuando:** `Reutilización` demuestra la búsqueda previa de componentes existentes, la spec está `reviewed` y cada AC tiene al menos un TEST.

### 8. Análisis de impacto

- **Entrada:** especificación técnica.
- **Salida:** bloque `impact` del frontmatter y sección `Análisis de impacto`; ADR si se introduce algo nuevo.
- **Salida lista cuando:** se recorrió el checklist de [04-trazabilidad-y-dependencias.md](04-trazabilidad-y-dependencias.md#procedimiento-de-análisis-de-impacto) y QA añadió la regresión resultante. `trace promote <ID> --to ready` pasa.

### 9. Implementación

- **Entrada:** feature `ready`.
- **Salida:** ramas y PR en los repos de código de `repos`, siguiendo el orden de `Orden de implementación`.
- **Salida lista cuando:** los PR cumplen [07-git-y-ci.md](07-git-y-ci.md). `trace promote <ID> --to in-progress` al empezar.

### 10. Pruebas automatizadas

- **Entrada:** `QA-PLAN.md` y código.
- **Salida:** tests con token `TEST-…`, rutas en la columna `Automatización`.
- **Salida lista cuando:** CI verde y `trace promote <ID> --to ready-for-qa` pasa (G-QA). Ver [06-automatizacion-de-pruebas.md](06-automatizacion-de-pruebas.md).

### 11. Validación QA

- **Entrada:** feature `ready-for-qa` desplegado en el entorno de QA.
- **Salida:** `Resultado` por TEST, `EVID-…` en `evidence/`, `BUG-…` por cada falla.
- **Salida lista cuando:** todos los TEST tienen resultado distinto de `pendiente`. QA ejecuta `trace promote <ID> --to in-validation` al empezar.

### 12. Regresión

- **Entrada:** `impact.regression_tests` y `impact.features_affected`.
- **Salida:** tabla `Regresión` con resultado por TEST ajeno.
- **Salida lista cuando:** todas las filas `pasa`.

### 13. Aprobación y release

- **Entrada:** feature `in-validation` sin fallas.
- **Salida:** `QA-PLAN.status: passed`, CHANGELOG `Validación = aprobado`; al liberar, tag `REL-YYYY.MM.N` y `released_in`.
- **Salida lista cuando:** `trace promote <ID> --to approved` y luego `--to released` pasan. Ver [03-estados-y-puertas.md](03-estados-y-puertas.md).

### 14. Seguimiento de cambios

- **Entrada:** nuevo requisito, ajuste de regla o incidencia.
- **Salida:** `CHG-###` (cambio de comportamiento) o `BUG-###` (desviación de lo especificado), nueva entrada de `CHANGELOG.md`.
- **Salida lista cuando:** el CHG llega a `released` y todos sus features afectados (directos e inducidos) registran la entrada. Ver [05-gestion-de-cambios.md](05-gestion-de-cambios.md).

## Siguiente paso

Asigna los identificadores: [02-identificadores.md](02-identificadores.md).
