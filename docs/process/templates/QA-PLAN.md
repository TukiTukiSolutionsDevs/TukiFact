---
feature: {{ID}}
spec_version: 1.0.0
status: draft
last_run: ""
updated: {{DATE}}
---

# Plan de QA — {{ID}} {{TITLE}}

<!-- Contrato: docs/process/traceability-schema.md §4.4. Dueño: QA.
     Los casos se diseñan desde FEATURE.md (AC, reglas, permisos), no desde el código.
     Diseño durante `analysis`, en paralelo a la TECHNICAL-SPEC; al terminar, status: ready (condición de G-READY 5).
     La columna Automatización la completa desarrollo (G-QA 8); Resultados y Evidencias, QA.
     Guía de niveles, token del TEST y evidencias: docs/process/06-automatizacion-de-pruebas.md -->

## Objetivo

<!-- Qué debe quedar demostrado para aprobar el feature. -->

## Alcance

<!-- Historias, AC, roles y plataformas (backend, front, mobile) que se validan. -->

## Fuera de alcance

<!-- Lo que no se valida en este plan y por qué. -->

## Criterios de entrada

<!-- Checklist. Base sugerida:
    - [ ] Feature en ready-for-qa (G-QA)
    - [ ] CI verde en todos los repos del feature
    - [ ] Build desplegado en el entorno de QA
    - [ ] Datos de prueba cargados -->

## Criterios de salida

<!-- Checklist. Base sugerida:
    - [ ] Todo TEST en pasa y toda fila de Regresión en pasa
    - [ ] EVID por cada TEST e2e y cada TEST manual
    - [ ] Ningún BUG critical/major abierto -->

## Entornos

| Entorno | URL o dispositivo | Versión / commit | Notas |
|---|---|---|---|

## Datos de prueba

<!-- Datos necesarios por caso, cómo se crean (builders, seeds, fixtures) y cómo se aíslan. Sin datos personales reales. -->

## Matriz de roles y permisos

| Rol | Acción | Esperado | TEST |
|---|---|---|---|

## Casos de prueba

| ID | Criterios | Tipo | Nivel | Repo | Automatización | Resultado |
|---|---|---|---|---|---|---|

<!-- Cada AC del feature citado por ≥1 TEST (G-READY 5). Ejemplo de fila (sin sangría):
    | TEST-{{MODULE}}-{{NUM}}-01 | AC-{{MODULE}}-{{NUM}}-01 | positivo | e2e | front | e2e/payments/register-payment.spec.ts | pendiente |
    | TEST-{{MODULE}}-{{NUM}}-02 | AC-{{MODULE}}-{{NUM}}-02 | negativo | integration | backend | tests/Marketjoya.Modules.<Modulo>.IntegrationTests/… | pendiente |
    | TEST-{{MODULE}}-{{NUM}}-03 | AC-{{MODULE}}-{{NUM}}-01, AC-{{MODULE}}-{{NUM}}-02 | permisos | e2e | manual | manual: requiere impresora fiscal | pendiente |
  Tipo ∈ positivo|negativo|limite|error|concurrencia|permisos|seguridad|rendimiento|accesibilidad|compatibilidad|regresion|recuperacion|migracion (ASCII, sin tildes)
  Nivel ∈ unit|domain|integration|database|contract|cross-module|messaging|component|e2e|visual|migration
  Repo ∈ backend|front|mobile|manual · Resultado ∈ pendiente|pasa|falla|bloqueado -->

## Regresión

| TEST | Feature | Motivo | Resultado |
|---|---|---|---|

<!-- Copia de impact.regression_tests de TECHNICAL-SPEC.md, con el feature dueño de cada TEST. -->

## No funcionales

<!-- Rendimiento, seguridad, accesibilidad, operación offline, compatibilidad. Cada uno con su TEST en Casos de prueba (Criterios = —) o No aplica — <motivo>. -->

## Evidencias

| ID | TEST | Archivo o enlace | Fecha |
|---|---|---|---|

<!-- Archivo relativo a evidence/ (p. ej. EVID-{{MODULE}}-{{NUM}}-01.png) o URL de una ejecución concreta de CI. -->

## Resultados

| Fecha | Entorno | Build / commit | Ejecutados | Pasan | Fallan | Bloqueados | Responsable |
|---|---|---|---|---|---|---|---|

<!-- Una fila por ejecución. Actualiza last_run y status (ready|executing|passed|failed). Cada falla enlaza su BUG. -->
