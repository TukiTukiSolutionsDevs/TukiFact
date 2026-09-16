---
id: {{ID}}
title: "{{TITLE}}"
feature: {{FEATURE}}
severity: major
status: open
source: qa
found_in: ""
tests: []
criteria: []
date: {{DATE}}
---

<!-- Contrato: docs/process/traceability-schema.md §4.7. Flujo: docs/process/05-gestion-de-cambios.md#flujo-de-incidencias-bug
     severity ∈ critical|major|minor|trivial · status ∈ open|in-progress|fixed|verified|closed|wontfix
     feature puede quedar vacío solo mientras status = open (p. ej. bug de producción sin triage); obligatorio desde in-progress
     source ∈ qa|ci|production|review · found_in = versión, commit o release
     tests: TEST que falla (o el que se agregará) · criteria: AC incumplidos -->

# {{ID}} — {{TITLE}}

## Descripción

<!-- Qué falla, en una o dos frases, y a quién afecta. -->

## Pasos para reproducir

<!-- Pasos numerados, con entorno, rol, datos y build. -->

## Resultado esperado

<!-- Lo que exige la fuente de verdad, citando el AC o la regla (BR). -->

## Resultado obtenido

<!-- Lo que ocurre realmente, con code de error y correlationId si los hay. -->

## Fuente de verdad

<!-- PRD | FEATURE | comportamiento. Cita el REQ, AC o BR exacto. Si la especificación es ambigua, no es BUG: abre una Q en el feature. -->

## Evidencia

<!-- EVID del feature, capturas, logs sin datos sensibles o enlace a una ejecución concreta de CI. -->

## Resolución

<!-- Causa raíz, commit/PR del fix (fix(<BUG-ID>): …), TEST que lo cubre y verificación de QA. Si es wontfix: justificación. -->
