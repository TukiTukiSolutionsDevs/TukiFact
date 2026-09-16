# Adopción progresiva en Market Real

**Se adopta en cinco fases, cada una utilizable por sí sola; CI empieza avisando y solo bloquea cuando el equipo ya produce documentos válidos.** Las bases ya construidas (arquitectura, contrato de errores, design system, autenticación base) no se reconvierten en features: se registran como ADR.

## Fases

| Fase | Objetivo | Entregables | Criterio de salida |
|---|---|---|---|
| 0. Convenciones | Todos usan el mismo idioma | Esquema, docs `docs/process/`, plantillas, plantilla de PR, `modules.yaml` vacío | Equipo y agentes conocen el camino rápido del [README](README.md) |
| 1. Producto | Saber qué se construye | `PRD.md`, `VIEWS.md`, `FUNCTIONAL-ANALYSIS.md`, `modules.yaml` con módulos decididos | Cada vista del maquetado tiene `VIEW-###`; cada REQ tiene módulo; preguntas de producto registradas |
| 2. Primer feature vertical | Probar el proceso de punta a punta | Un feature en backend + front (+ mobile si aplica) recorriendo los 14 pasos hasta `approved` | `trace promote <ID> --to approved` pasa; lecciones aprendidas incorporadas a plantillas |
| 3. CI | Hacer cumplir sin frenar | Workflow del repo padre en modo **aviso**, lint de commits en repos hijos; luego modo **bloqueo** | Dos semanas sin errores nuevos en modo aviso → se activa bloqueo |
| 4. Cambios y releases | Cerrar el ciclo | CHG, BUG, tags `REL-…`, `released_in` | Primera release con todos sus features en `released` y CHANGELOG completos |

### Fase 3 en detalle: de aviso a bloqueo

| Paso | Comportamiento de CI |
|---|---|
| 3a | `trace check` y lint corren con `continue-on-error: true`; los errores se ven en el PR |
| 3b | Bloquea lint de título de PR y `trace check` (formato e IDs) |
| 3c | Bloquea `trace build` sin diferencias y puertas del estado actual |
| 3d | Bloquea `trace check --code` (token del TEST presente) |

## Registro retroactivo de lo ya construido

| Ya existe | Cómo se registra | No se hace |
|---|---|---|
| Monolito modular .NET, Wolverine, un schema por módulo | ADR (`backend-architecture`) | Crear un feature "arquitectura backend" |
| Hexaclean en el front, Compose + Koin + Room en mobile | ADR por app | Historias de usuario sobre capas |
| Contrato de errores unificado | ADR que enlaza al contrato canónico | Duplicar el contrato en `docs/` |
| Design system central | ADR | Features por token |
| Autenticación/identidad base implementada | ADR de lo técnico; el comportamiento de negocio (login, roles) entra como feature cuando se decida el contrato de auth | Inventar AC para decisiones aún pendientes |
| Pruebas existentes (health, correlación, errores) | Siguen como están; no requieren TEST-ID | Renombrar tests existentes |

Reglas del registro retroactivo:

- [ ] Un ADR por decisión, con `status: accepted` y `date` real de la decisión si se conoce.
- [ ] Las decisiones pendientes de [AGENTS.md](../../AGENTS.md) (enums del API, sync offline, auth y hardware) **no** se registran como ADR aceptado: quedan como `proposed` o como preguntas del PRD.
- [ ] Un feature nuevo que se apoya en una base existente la cita en `adrs` y en `Reutilización`.

## Métricas de que funciona

| Métrica | Fuente | Señal sana |
|---|---|---|
| AC sin TEST | `matrix.md` | 0 en features `ready` o posteriores |
| TEST e2e/manual sin EVID en `approved` | `matrix.md` | 0 |
| Preguntas críticas abiertas por feature | `open-questions.md` | Tienden a 0 antes de `ready` |
| Features devueltos de `ready-for-qa`/`in-validation` a `in-progress` | historial Git de `FEATURE.md` | Bajan con el tiempo |
| BUG `critical`/`major` en producción por release | `docs/bugs/` (`source: production`) | Bajan con el tiempo |
| Commits sin ID válido | lint de CI | 0 tras la fase 3b |
| Tiempo de `ready` a `approved` | fechas `updated` + Git | Estable o decreciente |
| CHG con inducidos no detectados en análisis | CHG corregidos tras aprobación | 0 |

## Antipatrones

| Antipatrón | Por qué es un problema | Corrección |
|---|---|---|
| Escribir la documentación después del código | La doc describe lo hecho, no lo pedido; QA pierde independencia | `ready` antes de la primera línea de código |
| AC copiados del maquetado | Faltan reglas, permisos y errores | Checklist de descubrimiento de [01-flujo.md](01-flujo.md#checklist-de-descubrimiento-lo-que-el-maquetado-no-dice) |
| Inventar reglas para "no bloquear" | Se implementa algo que el negocio no pidió | `ASM-…` con `Validar con`, o `Q-…` crítica |
| Editar un AC para que el test pase | Oculta un bug | `BUG` o `Q` ([05](05-gestion-de-cambios.md#independencia-de-qa)) |
| Features gigantes ("módulo de ventas") | No se aprueban nunca | Un feature = una capacidad verificable |
| Editar `docs/traceability/` a mano | Se desincroniza | `trace build` |
| Numerar IDs a mano | Colisiones | `trace next-id` |
| Reintentos en CI para llegar a verde | Oculta inestabilidad | Política de flaky de [06](06-automatizacion-de-pruebas.md#política-de-pruebas-inestables-flaky) |
| Pasar a bloqueo en CI el primer día | El equipo desactiva el control | Fase 3 escalonada |
| Registrar arquitectura como features | Historias sin usuario ni valor verificable | ADR |
