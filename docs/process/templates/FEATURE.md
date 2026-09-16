---
id: {{ID}}
title: "{{TITLE}}"
module: {{MODULE}}
status: draft
spec_version: 1.0.0
owners: { analyst: "", architect: "", developer: "", qa: "" }
prd_requirements: []
views: []
depends_on: []
related: []
repos: []
blocked_reason: ""
blocked_from: ""
target_release: ""
released_in: ""
updated: {{DATE}}
---

# {{ID}} — {{TITLE}}

<!-- Contrato: docs/process/traceability-schema.md §4.2. Guía: docs/process/01-flujo.md (etapas 5 y 6).
     Los comentarios HTML se eliminan antes de analizar: los ejemplos van aquí, con sangría ({{NUM}} = número de 3 dígitos del feature).
     Pide cada número con: trace next-id US|AC|BR|Q|ASM --feature {{ID}}
     No edites status, blocked_reason ni blocked_from a mano: usa trace promote {{ID}} --to <estado> [--reason "…"].
     Valores enumerados en ASCII sin tilde (si|no, limite…); el texto libre sí lleva tildes. -->

## Objetivo

<!-- Una o dos frases: qué capacidad obtiene el usuario y qué valor de negocio aporta. -->

## Problema

<!-- Situación actual y por qué es un problema. Cita los REQ del PRD de los que nace. -->

## Alcance

<!-- Lista de lo que SÍ incluye este feature. -->

## Fuera de alcance

<!-- Lista explícita de lo que NO incluye (y en qué feature o CHG se tratará, si se sabe). -->

## Actores, roles y permisos

| Rol | Puede | No puede |
|---|---|---|

<!-- Un rol por fila. Los permisos se aplican en el backend; la UI solo oculta. -->

## Precondiciones

<!-- Qué debe ser cierto antes de iniciar el flujo principal (sesión, caja abierta, datos existentes…). -->

## Flujo principal

<!-- Pasos numerados, actor → sistema. Cita las vistas por su VIEW. -->

## Flujos alternativos

<!-- Cada flujo alternativo o de error con su disparador: sin conexión, sin stock, duplicado, sesión vencida, cancelación… -->

## Estados

| Estado | Descripción | Transiciones permitidas |
|---|---|---|

<!-- Estados de la entidad principal. Si no hay estados: No aplica — <motivo>. -->

## Reglas de negocio

| ID | Regla | Origen |
|---|---|---|

<!-- Origen = REQ, ASM o CHG. Ejemplo de fila (sin sangría):
    | BR-{{MODULE}}-{{NUM}}-01 | El monto del pago no puede superar el saldo pendiente | REQ-### | -->

## Validaciones

| Campo | Regla | code | Mensaje |
|---|---|---|---|

<!-- code según el contrato de errores: <Campo>.<Regla>, p. ej. Amount.GreaterThanZero. -->

## Mensajes de error

| code | type | Mensaje al usuario | Cuándo |
|---|---|---|---|

<!-- type ∈ VALIDATION|UNAUTHORIZED|FORBIDDEN|NOT_FOUND|CONFLICT|FAILURE|NETWORK|UNKNOWN.
     code con formato <Ámbito>.<Motivo> (.agents/skills/backend-architecture/references/api-error-contract.md). -->

## Historias de usuario

<!-- Formato exacto (quitar la sangría al usar). Cada historia con ≥1 AC; cada AC con Given, When y Then en ese orden; And permitido.

    ### US-{{MODULE}}-{{NUM}}-01 — Registrar pago total
    Como cajero, quiero registrar el pago total de una venta, para cerrarla y emitir el comprobante.

    #### AC-{{MODULE}}-{{NUM}}-01 — Pago total cierra la venta
    Given una venta con saldo pendiente de 50.00
    When el cajero registra un pago de 50.00
    Then la venta queda en estado pagada
    And el saldo pendiente es 0.00

    #### AC-{{MODULE}}-{{NUM}}-02 — Rechaza monto mayor al saldo
    Given una venta con saldo pendiente de 50.00
    When el cajero registra un pago de 60.00
    Then el sistema rechaza el pago con el code Payment.AmountExceedsBalance
-->

## Dependencias y features relacionados

<!-- Coherente con depends_on y related del frontmatter. Una línea por feature:
    - Depende de: <MOD>-FEAT-NNN — motivo
    - Relacionado: <MOD>-FEAT-NNN — qué consume o qué le afecta -->

## Vistas de referencia

<!-- Coherente con views del frontmatter. Una línea por vista (la declaración vive en docs/product/VIEWS.md):
    - VIEW-### — nombre — qué parte del flujo cubre -->

## Suposiciones

| ID | Suposición | Validar con | Estado |
|---|---|---|---|

<!-- Estado ∈ pendiente|confirmada|descartada. Ejemplo:
    | ASM-{{MODULE}}-{{NUM}}-01 | El pago parcial no aplica a ventas a crédito | Negocio (jefe de caja) | pendiente | -->

## Preguntas pendientes

| ID | Pregunta | Crítica | Estado | Respuesta |
|---|---|---|---|---|

<!-- Crítica ∈ si|no (ASCII); Estado ∈ abierta|resuelta. Una pregunta crítica abierta bloquea G-READY. Ejemplo:
    | Q-{{MODULE}}-{{NUM}}-01 | ¿Hay tope de pagos parciales por venta? | si | abierta | | -->
