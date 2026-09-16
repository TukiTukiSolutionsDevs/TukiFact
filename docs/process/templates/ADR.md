---
id: {{ID}}
title: "{{TITLE}}"
status: proposed
date: {{DATE}}
features: [{{FEATURE}}]
supersedes: ""
superseded_by: ""
---

<!-- Contrato: docs/process/traceability-schema.md §4.8. Dueño: arquitecto.
     status ∈ proposed|accepted|superseded|deprecated. Una decisión por ADR.
     Las decisiones marcadas como pendientes en AGENTS.md no se aceptan sin decisión explícita del responsable. -->

# {{ID}} — {{TITLE}}

## Contexto

<!-- Fuerzas en juego: requisito, restricción técnica, problema observado. Enlaza features, REQ y specs. -->

## Decisión

<!-- La decisión en voz activa: "Usamos …", "No usamos …". -->

## Alternativas consideradas

| Alternativa | A favor | En contra | Motivo de descarte |
|---|---|---|---|

## Consecuencias

<!-- Positivas, negativas y lo que queda obligado a partir de ahora (convenciones, pruebas, operación). -->

## Justificación de lo nuevo

<!-- Obligatoria si el ADR introduce servicio, patrón, cola, evento o mecanismo de comunicación.
     Responde cada pregunta en su H3, en este orden; `No aplica — <motivo>` permitido. -->

### 1. ¿Qué problema resuelve?

### 2. ¿Por qué no puede reutilizarse la arquitectura existente?

<!-- Qué se buscó y dónde (rutas, módulos, skills de arquitectura). -->

### 3. ¿Qué módulos participan?

### 4. ¿Qué contratos utiliza?

<!-- OpenAPI operationId, evento de integración, tabla, puerto. -->

### 5. ¿Qué errores pueden producirse?

<!-- Con `code` del contrato de errores (.agents/skills/backend-architecture/references/api-error-contract.md). -->

### 6. ¿Cómo se recupera de esos errores?

<!-- Reintentos, idempotencia, DLQ, compensación. -->

### 7. ¿Cómo se monitorea?

<!-- Trazas, métricas, logs, alertas. -->

### 8. ¿Cómo se prueba?

<!-- TEST y niveles. -->

### 9. ¿Qué impacto tiene en otros features?

<!-- IDs de features. -->
