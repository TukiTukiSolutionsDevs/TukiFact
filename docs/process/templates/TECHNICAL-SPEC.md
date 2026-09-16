---
feature: {{ID}}
spec_version: 1.0.0
status: draft
adrs: []
impact:
  modules_direct: [{{MODULE}}]
  modules_indirect: []
  features_affected: []
  regression_tests: []
  contracts: []
  migrations: false
updated: {{DATE}}
---

# Especificación técnica — {{ID}} {{TITLE}}

<!-- Contrato: docs/process/traceability-schema.md §4.3. Dueño: arquitecto.
     Todas las secciones son obligatorias desde `ready`. Si una no aplica, escribe: No aplica — <motivo>.
     No cambies reglas ni AC aquí: se proponen en el traspaso al analista. -->

## Resumen

<!-- Qué se construye técnicamente, en 3–5 líneas, y en qué repos. -->

## Arquitectura actual involucrada

<!-- Módulos, casos de uso, endpoints, tablas y pantallas existentes que participan. Enlaza a la skill de arquitectura del repo. -->

## Reutilización

<!-- Obligatorio buscar antes de proponer algo nuevo. Lista: qué se buscó (rutas, rg), qué se reutiliza y qué se descartó y por qué. -->

## Componentes nuevos

<!-- Solo lo que no existe. Todo servicio, patrón, cola, evento o comunicación nueva exige ADR con las 9 preguntas. -->

## Backend

<!-- Módulo, Commands/Queries, agregados, validadores, endpoints (/api/v1), Result y errores. Skill: backend-architecture. -->

## Frontend

<!-- Features hexaclean: puertos, casos de uso, adapters, facades, pantallas, componentes Mr*. Skill: hexaclean-architecture. -->

## Mobile

<!-- Módulos Gradle, UseCases, Room, outbox/sync, pantallas Compose, hardware. Skill: mobile-architecture. -->

## Base de datos y migraciones

<!-- Schema del módulo, tablas, índices, migraciones EF (expand/contract). Actualiza impact.migrations. -->

## Contratos

<!-- operationIds del OpenAPI (MarketjoyaBackend/openapi/marketjoya-api-v1.json) creados o modificados; compatibilidad con clientes. Registra en impact.contracts como openapi:<operationId>. -->

## Eventos y mensajería

<!-- Eventos de integración Wolverine producidos/consumidos, outbox/inbox, idempotencia. Registra en impact.contracts como event:<Nombre>. -->

## Integraciones externas

<!-- SUNAT, OCR, GPS u otras: puerto, adapter, timeouts, reintentos, sustitución en pruebas. -->

## Seguridad y autorización

<!-- Políticas de autorización en backend, datos sensibles, auditoría, secretos fuera del cliente. -->

## Observabilidad

<!-- Logs, métricas y trazas; propagación de X-Correlation-Id. -->

## Errores

<!-- Tabla code → type → HTTP de los errores nuevos, alineada con "Mensajes de error" de FEATURE.md y con el contrato de errores. -->

## Concurrencia y transacciones

<!-- Límites transaccionales, sync_version / Concurrency.Conflict, client_mutation_id, doble envío. -->

## Compatibilidad y datos existentes

<!-- Versiones de API, clientes móviles desactualizados, datos previos, backfill. -->

## Despliegue y reversión

<!-- Orden de despliegue (migraciones antes del rollout), feature flags si existen, cómo revertir. -->

## Riesgos

<!-- Riesgo → probabilidad → impacto → mitigación. -->

## Análisis de impacto

<!-- Recorre el checklist de docs/process/04-trazabilidad-y-dependencias.md: módulos directos/indirectos, consumidores de datos,
     endpoints y tablas compartidos, eventos, jobs, permisos, reportes, notificaciones, integraciones, regresión,
     compatibilidad, rendimiento y seguridad. Cada punto: hallazgo o No aplica — <motivo>. Refleja el resultado en `impact`. -->

## Archivos a crear o modificar

<!-- Por repo, rutas relativas al repo:
    - backend: src/Modules/<Modulo>/…
    - front: src/…
    - mobile: <módulo>/src/main/… -->

## Orden de implementación

<!-- Pasos numerados y verificables; cada paso indica repo y los AC/TEST que habilita. Equivale a las tareas de sdd-tasks. -->
