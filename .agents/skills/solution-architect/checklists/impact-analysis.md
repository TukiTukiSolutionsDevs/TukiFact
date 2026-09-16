# Checklist: análisis de impacto

Resultado en el frontmatter `impact` y en el H2 `Análisis de impacto` de `TECHNICAL-SPEC.md`. Cada casilla: hallazgo con ruta o `No aplica — <motivo>`. Comandos de búsqueda: [../references/reuse-search.md](../references/reuse-search.md).

## Módulos

- [ ] `impact.modules_direct`: módulos cuyo código cambia (no vacío).
- [ ] `impact.modules_indirect`: módulos que consumen datos, contratos o eventos que cambian.

## Datos

- [ ] Tablas y schemas Postgres tocados (`HasDefaultSchema`, configuraciones EF); quién más los lee o escribe.
- [ ] Read models, vistas o consultas de otros módulos sobre esos datos.
- [ ] Colecciones Mongo (auditoría, dead letters) afectadas.
- [ ] Room en mobile (`*Entity`, `*Dao`, `core/database/schemas`): versión y migración.
- [ ] `impact.migrations`: `true` si cambia esquema o datos existentes; plan de backfill y reversión.

## Contratos

- [ ] Endpoints nuevos o modificados en `MarketjoyaBackend/openapi/marketjoya-api-v1.json` (operationId → `impact.contracts` como `openapi:<operationId>`).
- [ ] Clientes que ya consumen esos operationIds (front y mobile).
- [ ] Compatibilidad: campo nuevo opcional, campo removido o renombrado (ruptura), enums (decisión pendiente: string o número).
- [ ] `code` de error nuevos alineados al contrato de errores.

## Eventos y mensajería

- [ ] Integration events producidos (`Marketjoya.BuildingBlocks.Contracts`) → `impact.contracts` como `event:<Tipo>`.
- [ ] Consumers existentes de esos eventos (`IIntegrationEventConsumer<T>`) en cualquier módulo.
- [ ] Versión del evento si cambia su forma; consumers deduplican por `Id`.
- [ ] Domain events y handlers afectados dentro del módulo.

## Procesos y operación

- [ ] Jobs, tareas recurrentes, reintentos, sync offline (`client_mutation_id`, outbox mobile).
- [ ] Permisos: nuevos `RequireAuthorization("<permiso>")`, roles que los reciben, pantallas que los usan.
- [ ] Reportes y exportaciones que leen los datos cambiados.
- [ ] Notificaciones disparadas o dejadas de disparar.
- [ ] Integraciones externas (SUNAT, OCR, GPS, hardware POS).

## Features y regresión

- [ ] Features existentes afectados (`docs/traceability/dependency-map.md`, `rg` de IDs en `docs/features`) → `impact.features_affected`.
- [ ] `depends_on` (dependencia dura) y `related` (consume o se ve afectado) actualizados en `FEATURE.md`.
- [ ] `TEST-*` de esos features a re-ejecutar → `impact.regression_tests`.
- [ ] Features afectados en `≥ ready` → `CHG-###` inducido.

## No funcionales

- [ ] Rendimiento: volumen, consultas N+1, índices, tamaño de payload en redes móviles.
- [ ] Concurrencia y transacciones: bloqueo optimista, idempotencia, doble envío.
- [ ] Seguridad: autorización en backend, datos sensibles, secretos fuera del cliente.
- [ ] Observabilidad: logs, trazas, `X-Correlation-Id` extremo a extremo.
- [ ] Despliegue: orden entre repos, feature flag si hay ruptura, reversión.
