# Trazabilidad, dependencias e impacto

**La matriz y el mapa de dependencias se generan con `trace build` a partir de los documentos; nadie los edita.** Antes de tocar un feature se hace el análisis de impacto de este documento, y un feature no se aprueba si la documentación de quienes dependen de él quedó desactualizada.

## Camino rápido

1. Actualiza `depends_on`, `related` (FEATURE) e `impact` (TECHNICAL-SPEC).
2. `trace build` → revisa el diff de `docs/traceability/`.
3. Commitea los archivos generados en el mismo PR; CI falla si difieren.

## Matriz de trazabilidad

Archivo: `docs/traceability/matrix.md`. Una fila por AC.

| Columna | Origen |
|---|---|
| `REQ` | `prd_requirements` del feature |
| `Feature` | `id` de `FEATURE.md` |
| `US` | historia que contiene el AC |
| `AC` | encabezado del criterio |
| `TEST` | filas de `Casos de prueba` cuyo `Criterios` cita el AC |
| `Nivel` | columna `Nivel` del TEST |
| `Automatización` | ruta del test o `manual: …` |
| `Resultado` | columna `Resultado` del TEST |
| `EVID` | filas de `Evidencias` que citan el TEST |
| `Estado feature` | `status` |
| `Versión` | `spec_version` |
| `Release` | `released_in` |

Ejemplo de filas:

| REQ | Feature | US | AC | TEST | Nivel | Automatización | Resultado | EVID | Estado feature | Versión | Release |
|---|---|---|---|---|---|---|---|---|---|---|---|
| REQ-012 | PAY-FEAT-001 | US-PAY-001-01 | AC-PAY-001-01 | TEST-PAY-001-01, TEST-PAY-001-02 | integration, e2e | tests/…/RegisterPaymentTests.cs, e2e/payments/register.spec.ts | pasa | EVID-PAY-001-01 | approved | 1.1.0 | — |
| REQ-012 | PAY-FEAT-001 | US-PAY-001-01 | AC-PAY-001-03 | TEST-PAY-001-07 | domain | tests/…/PaymentTests.cs | falla | — | in-validation | 1.1.0 | — |

Cómo leerla: un AC sin TEST, un TEST `falla` o un TEST e2e sin EVID son huecos visibles de un vistazo.

Otros archivos generados: `dependency-map.md` (diagrama y tabla inversa "quién depende de mí") y `open-questions.md` (preguntas y suposiciones abiertas, críticas primero).

## Modelo de dependencias

| Relación | Dónde | Significado | Efecto en puertas |
|---|---|---|---|
| `depends_on` | `FEATURE.md` | Sin el otro feature, este **no funciona**. | G-APPROVED 13: el dependido debe estar `approved`/`released`, o en `ready-for-qa`/`in-validation` con el mismo `target_release`. Línea sólida en el mapa. |
| `related` | `FEATURE.md` | Consume o es afectado por el otro, **sin dependencia dura**. | Solo existencia (G-READY 6). Línea punteada. |
| `impact.features_affected` | `TECHNICAL-SPEC.md` | Features cuya **documentación o pruebas cambian** por esta implementación. | Existencia (G-READY 6); obliga a actualizar su doc y a regresión. |
| `impact.regression_tests` | `TECHNICAL-SPEC.md` | `TEST-…` de otros features que se re-ejecutan. | Se copian a la tabla `Regresión`; G-APPROVED 9. |
| Cambio inducido | `CHG-###` → `features[].impact: induced` + `via` | El feature cambia **como consecuencia** de otro. | G-APPROVED 14: entrada en su CHANGELOG. |

Guía de decisión:

- ¿Falla en ejecución si el otro no existe? → `depends_on`.
- ¿Lee sus datos o reacciona a sus eventos, pero funciona sin él? → `related`.
- ¿Este trabajo obliga a reescribir AC, pruebas o doc del otro? → `impact.features_affected` y, si ya está liberado, un `CHG` con `impact: induced`.

Ejemplo: `NOTIF-FEAT-002` (notificar pago) consume `PaymentRegisteredIntegrationEvent`. Para `PAY-FEAT-001` es `related`; si `CHG-004` cambia el evento, `NOTIF-FEAT-002` aparece en el CHG como `induced` con `via: PAY-FEAT-001`.

## Procedimiento de análisis de impacto

Dueño: arquitecto. Resultado: bloque `impact` y sección `Análisis de impacto` de `TECHNICAL-SPEC.md`. Cada punto se responde con un hallazgo o `No aplica — <motivo>`.

- [ ] **Módulos directos** (`impact.modules_direct`): dónde se escribe código.
- [ ] **Módulos indirectos** (`impact.modules_indirect`): quién reacciona sin cambiar su código.
- [ ] **Consumidores de los datos modificados**: queries, reportes y pantallas que leen lo que cambia.
- [ ] **Endpoints compartidos**: `operationId` afectados en `MarketjoyaBackend/openapi/marketjoya-api-v1.json`; qué clientes (front, mobile) los consumen. Registrar como `openapi:<operationId>` en `impact.contracts`.
- [ ] **Tablas y schemas compartidos**: schema Postgres del módulo, lecturas Dapper de otros módulos, migraciones (`impact.migrations`).
- [ ] **Eventos producidos y consumidos** (Wolverine): contratos de integración, consumidores, outbox/inbox. Registrar como `event:<Nombre>`.
- [ ] **Procesos programados**: jobs, cierres, reintentos, sincronización offline del móvil.
- [ ] **Permisos**: roles nuevos o modificados, políticas de autorización del backend.
- [ ] **Reportes** que agregan los datos afectados.
- [ ] **Notificaciones** que se disparan o cambian.
- [ ] **Integraciones externas**: SUNAT, OCR, GPS.
- [ ] **Pruebas de regresión**: `TEST-…` ajenos que cubren lo anterior (`impact.regression_tests`).
- [ ] **Compatibilidad**: versiones del API (`/api/v1`), clientes móviles desactualizados, datos existentes, expand/contract en migraciones.
- [ ] **Contrato de errores**: `code` nuevos o retirados según el [contrato de errores](../../.agents/skills/backend-architecture/references/api-error-contract.md).
- [ ] **Rendimiento y seguridad**: volumen, bloqueos, exposición de datos, auditoría.

Fuentes para responder rápido:

| Pregunta | Dónde mirar |
|---|---|
| ¿Quién depende de este feature? | `docs/traceability/dependency-map.md` (tabla inversa) |
| ¿Quién usa este endpoint o evento? | `rg -n '<operationId>\|<Evento>' docs/features` sobre `impact.contracts` |
| ¿Qué pruebas cubren esta regla? | `docs/traceability/matrix.md` |
| ¿Qué módulos toca? | `Archivos a crear o modificar` de las specs relacionadas |

## Regla de documentación vigente de dependientes

Un feature **no puede aprobarse** si algún feature de `impact.features_affected`, o listado como `induced` en un CHG del feature, tiene documentación desactualizada. En la práctica:

- [ ] Cada feature afectado tiene su `spec_version` incrementado y una entrada de CHANGELOG que cita el CHG (G-APPROVED 14).
- [ ] Sus AC y TEST afectados están modificados en el mismo CHG, no en uno posterior.
- [ ] Sus TEST de regresión figuran en la tabla `Regresión` con `pasa` (G-APPROVED 9).
- [ ] `trace build` no produce diferencias.

Siguiente: cómo se registran esos cambios en [05-gestion-de-cambios.md](05-gestion-de-cambios.md).
