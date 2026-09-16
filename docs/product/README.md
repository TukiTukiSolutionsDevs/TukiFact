# Producto

**Aquí vive la definición del producto que da origen a los features: requisitos, vistas, análisis funcional y mapa de módulos.** Hoy solo existe `modules.yaml` vacío; el resto se crea desde plantillas cuando llegue el PRD real.

## Archivos

| Archivo | Contenido | Declara | Dueño | Plantilla | Estado |
|---|---|---|---|---|---|
| [`modules.yaml`](modules.yaml) | Código, slug, nombre y repos de cada módulo | Módulos | Arquitecto | — | Vacío: mapa de módulos pendiente de decisión |
| `PRD.md` | Visión, roles, requisitos funcionales y no funcionales, reglas, integraciones, glosario, preguntas | `REQ-###`, `Q-PRD-###` | Analista | [PRD.md](../process/templates/PRD.md) | Por crear |
| `RELEASES.md` | Registro de releases: fecha, repos y tags, features y CHG incluidos | `REL-YYYY.MM.N` | Responsable de release | [RELEASES.md](../process/templates/RELEASES.md) | Por crear con la primera release |
| `VIEWS.md` | Inventario de vistas y flujos con enlace al maquetado | `VIEW-###`, `FLOW-###` | Analista | [VIEWS.md](../process/templates/VIEWS.md) | Por crear |
| `FUNCTIONAL-ANALYSIS.md` | Por vista: propósito, campos, acciones, estados, reglas visibles, errores, permisos, inferido vs por confirmar | — (solo referencia vistas) | Analista | [FUNCTIONAL-ANALYSIS.md](../process/templates/FUNCTIONAL-ANALYSIS.md) | Por crear |

## Cuándo llega el PRD

1. Copia las tres plantillas a esta carpeta y reemplaza `{{DATE}}`.
2. Registra cada pantalla del maquetado en `VIEWS.md` (etapa 1 de [01-flujo.md](../process/01-flujo.md)).
3. Analiza cada vista en `FUNCTIONAL-ANALYSIS.md` con el checklist de descubrimiento (etapa 2).
4. Redacta los `REQ-###` en `PRD.md` (etapa 3).
5. El arquitecto completa `modules.yaml` (etapa 4). Solo entonces se crean features.

## Reglas

- No se inventan módulos, requisitos ni reglas: lo desconocido va a `Preguntas abiertas` de `PRD.md`.
- Un ID de requisito o vista no se renumera ni se borra; se marca como retirado.
- Si `PRD.md` o `VIEWS.md` existen, todo `REQ`/`VIEW` citado por un feature debe existir aquí (G-READY 7).
- Cambios en requisitos que ya alimentan features en `ready` o posterior se hacen con un `CHG` ([05-gestion-de-cambios.md](../process/05-gestion-de-cambios.md)).
