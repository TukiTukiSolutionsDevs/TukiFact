---
id: REPORT-FEAT-001
title: "Reporte de sesiones de caja"
module: REPORT
status: released
spec_version: 1.0.1
owners: { analyst: "analista (ejemplo)", architect: "arquitecto (ejemplo)", developer: "dev-backend y dev-front (ejemplo)", qa: "qa (ejemplo)" }
prd_requirements: [REQ-004]
views: [VIEW-003]
depends_on: []
related: [CASH-FEAT-001]
repos: [backend, front]
blocked_reason: ""
blocked_from: ""
target_release: ""
released_in: REL-2026.10.1
updated: 2026-10-02
---

# REPORT-FEAT-001 — Reporte de sesiones de caja

> **Ejemplo ilustrativo.** Feature relacionado del ejemplo `pos-cash-opening`: consume el hecho "sesión abierta" de CASH-FEAT-001 y recibe un cambio inducido (CHG-001, versión 1.0.1). Sus reglas son suposiciones confirmadas solo dentro del ejemplo.

## Objetivo

El supervisor consulta las sesiones de caja abiertas en una fecha, para controlar el efectivo inicial de cada caja de su tienda.

## Problema

Sin un reporte, el supervisor debe llamar a cada caja para saber si abrió y con cuánto (REQ-004).

## Alcance

- Reporte web de sesiones abiertas por fecha, con filtro opcional por caja.
- Columnas: caja, hora de apertura y monto inicial.

## Fuera de alcance

- Exportación a Excel (Q-REPORT-001-01).
- Nombre del cajero: requiere datos de usuarios de otro módulo (ver análisis funcional de VIEW-003).
- Aperturas pendientes de aprobación o rechazadas (CHG-001): no son sesiones abiertas.
- Cierres, arqueos y diferencias de caja.

## Actores, roles y permisos

| Rol | Puede | No puede |
|---|---|---|
| Supervisor | Ver el reporte de las cajas de su tienda | Ver cajas de otras tiendas |
| Cajero | — | Ver el reporte |

## Precondiciones

- El supervisor inició sesión en la consola web con permiso de reportes.

## Flujo principal

1. El supervisor abre "Reporte de sesiones de caja" (VIEW-003); la fecha por defecto es hoy (hora de Lima).
2. Opcionalmente elige una caja.
3. El sistema muestra las sesiones abiertas que cumplen los filtros, de la más reciente a la más antigua.

## Flujos alternativos

- A1 Sin resultados: se muestra "No hay sesiones de caja para los filtros elegidos".
- A2 Apertura hecha sin conexión: aparece cuando el POS la sincroniza.
- A3 Error inesperado: mensaje genérico con referencia de soporte.
- A4 Apertura que requirió aprobación (1.0.1, CHG-001): aparece cuando el supervisor la aprueba, con la hora de aprobación; si se rechaza, no aparece.

## Estados

No aplica — el reporte es de solo lectura; los estados de la sesión los define CASH-FEAT-001.

## Reglas de negocio

| ID | Regla | Origen |
|---|---|---|
| BR-REPORT-001-01 | El reporte muestra solo sesiones abiertas de la tienda del supervisor | REQ-004, ASM-REPORT-001-01 |
| BR-REPORT-001-02 | Fecha y hora de apertura se muestran en hora de Lima; en una sesión aprobada por supervisor, la hora de apertura es la de la aprobación | REQ-004, CHG-001 |
| BR-REPORT-001-03 | Una sesión aparece una sola vez aunque su apertura se reciba más de una vez | REQ-004 |

## Validaciones

| Campo | Regla | code | Mensaje |
|---|---|---|---|
| date | Obligatorio, fecha válida | Date.Invalid | "Elige una fecha válida" |
| cashRegisterId | Opcional; si viene, identificador válido | CashRegisterId.Invalid | "La caja elegida no es válida" |

## Mensajes de error

| code | type | Mensaje al usuario | Cuándo |
|---|---|---|---|
| GetCashSessionReport.Validation | VALIDATION | Mensaje en cada filtro | Filtro inválido |
| Auth.Forbidden | FORBIDDEN | "No tienes permiso para ver este reporte" | Usuario sin permiso de reportes |
| Server.Failure | FAILURE | "No pudimos cargar el reporte. Referencia: <correlationId>" | Error inesperado |

## Historias de usuario

### US-REPORT-001-01 — Consultar sesiones de caja por fecha
Como supervisor, quiero ver las sesiones de caja abiertas en una fecha, para controlar el efectivo inicial de cada caja.

#### AC-REPORT-001-01 — Una sesión abierta aparece en el reporte del día
Given "Caja 01" quedó abierta hoy a las 08:05 (hora de Lima) con S/ 200.00, sea al abrirla o al aprobarla un supervisor a esa hora
When el supervisor consulta el reporte de hoy
Then el reporte muestra una fila de "Caja 01" con hora 08:05 y monto S/ 200.00

#### AC-REPORT-001-02 — Filtra por caja
Given hoy quedaron abiertas "Caja 01" y "Caja 02"
When el supervisor consulta el reporte de hoy filtrando por "Caja 02"
Then el reporte muestra solo la sesión de "Caja 02"

#### AC-REPORT-001-03 — Usuario sin permiso de reportes no accede
Given un usuario con rol cajero sin permiso de reportes
When intenta consultar el reporte de sesiones de caja
Then el reporte no se muestra
And se muestra "No tienes permiso para ver este reporte"

## Dependencias y features relacionados

- Relacionado: CASH-FEAT-001 — origen del hecho "sesión abierta"; el reporte funciona sin él, pero vacío. Cambio inducido recibido: CHG-001 (1.0.1).

## Vistas de referencia

- VIEW-003 — Reporte de sesiones de caja — flujo completo.

## Suposiciones

| ID | Suposición | Validar con | Estado |
|---|---|---|---|
| ASM-REPORT-001-01 | El supervisor ve solo las cajas de su tienda | Jefatura de caja (ejemplo) | confirmada |
| ASM-REPORT-001-02 | Un retraso de hasta 1 minuto entre la apertura y su aparición en el reporte es aceptable | Jefatura de caja (ejemplo) | confirmada |

## Preguntas pendientes

| ID | Pregunta | Crítica | Estado | Respuesta |
|---|---|---|---|---|
| Q-REPORT-001-01 | ¿El reporte debe exportarse a Excel? | no | abierta | |
