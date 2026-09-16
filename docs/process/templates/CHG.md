---
id: {{ID}}
title: "{{TITLE}}"
status: proposed
requested_by: ""
date: {{DATE}}
features: []
requirements: []
released_in: ""
---

<!-- Contrato: docs/process/traceability-schema.md §4.6. Proceso: docs/process/05-gestion-de-cambios.md
     status ∈ proposed|analysis|approved|implemented|verified|released|rejected
     Formato de features (reemplaza `features: []`):
       features:
         - id: {{FEATURE}}
           impact: direct        # direct | induced
           bump: minor           # major | minor | patch
         - id: <MOD>-FEAT-NNN
           impact: induced
           via: {{FEATURE}}
           bump: patch -->

# {{ID}} — {{TITLE}}

## Motivo

<!-- Quién lo pide, qué necesidad de negocio lo origina y qué pasa si no se hace. -->

## Requisitos que cambian

<!-- REQ nuevos o modificados en PRD.md, con el texto anterior y el nuevo. Coherente con `requirements`. -->

## Impacto técnico

<!-- Resultado del checklist de análisis de impacto (docs/process/04-trazabilidad-y-dependencias.md): módulos, contratos OpenAPI,
     eventos, tablas y migraciones, permisos, integraciones, compatibilidad. -->

## Features afectados

| Feature | Impacto | Vía | Bump | Versión anterior → nueva | AC afectados |
|---|---|---|---|---|---|

<!-- Una fila por entrada de `features`. AC afectados: ID (modificado|nuevo|eliminado). -->

## Plan de pruebas y regresión

<!-- TEST nuevos, modificados y de regresión por feature; niveles y repos. -->

## Evidencias

<!-- EVID de los features afectados o enlaces a ejecuciones de CI concretas. -->

## Decisión

<!-- Aprobado o rechazado, quién decide, fecha y condiciones. -->
