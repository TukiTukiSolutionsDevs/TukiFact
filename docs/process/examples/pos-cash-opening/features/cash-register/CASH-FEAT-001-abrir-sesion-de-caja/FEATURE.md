---
id: CASH-FEAT-001
title: "Abrir sesión de caja"
module: CASH
status: released
spec_version: 1.1.0
owners: { analyst: "analista (ejemplo)", architect: "arquitecto (ejemplo)", developer: "dev-backend, dev-mobile y dev-front (ejemplo)", qa: "qa (ejemplo)" }
prd_requirements: [REQ-001, REQ-002, REQ-005, REQ-006, REQ-007, REQ-008]
views: [VIEW-001, VIEW-002]
depends_on: [AUTH-FEAT-001]
related: [REPORT-FEAT-001]
repos: [backend, mobile, front]
blocked_reason: ""
blocked_from: ""
target_release: ""
released_in: REL-2026.10.1
updated: 2026-10-02
---

# CASH-FEAT-001 — Abrir sesión de caja

> **Ejemplo ilustrativo.** Las reglas de negocio reales de Market Real no están definidas. Los montos, límites y comportamientos de este feature son suposiciones (`ASM-…`) y respuestas (`Q-…`, `Q-PRD-…`) confirmadas **dentro del ejemplo** para mostrar el proceso; no son decisiones del producto. Versión 1.1.0: incluye CHG-001.

## Objetivo

El cajero abre la sesión de su caja registrando el efectivo inicial, con o sin conexión, para empezar a vender con un saldo de arranque conocido y auditado; las aperturas con monto alto las aprueba un supervisor.

## Problema

En el escenario del ejemplo el efectivo inicial se anota en papel: no se sabe qué cajas están abiertas ni con cuánto dinero, y dos cajeros pueden operar la misma caja. Nace de REQ-001 y REQ-002, con las exigencias de operación sin conexión (REQ-005), rendimiento (REQ-006) y auditoría (REQ-007). Desde 1.1.0, REQ-008 exige aprobación de supervisor para montos mayores a S/ 500.00.

## Alcance

- Apertura de la sesión de la caja asignada al POS con monto inicial en soles.
- Apertura sin conexión con sincronización posterior (hasta S/ 500.00 desde 1.1.0).
- Una sola sesión abierta por caja, también ante reintentos o dos dispositivos.
- Aviso del hecho "sesión abierta" para que otros módulos (reportes) lo conozcan.
- Aprobación o rechazo por un supervisor de las aperturas mayores a S/ 500.00 desde la consola web (VIEW-002) — 1.1.0, CHG-001.

## Fuera de alcance

- Aprobación sin conexión (ASM-CASH-001-04).
- Notificaciones al supervisor; la consola muestra las pendientes al consultarla.
- Cierre y arqueo de caja.
- Impresión de comprobante de apertura (Q-CASH-001-02).
- Catálogo y asignación de cajas a dispositivos (la caja llega en la sesión de AUTH-FEAT-001).

## Actores, roles y permisos

| Rol | Puede | No puede |
|---|---|---|
| Cajero | Abrir la sesión de la caja asignada a su sesión del POS | Elegir otra caja; abrir una caja ocupada; aprobar o rechazar aperturas |
| Supervisor | Aprobar o rechazar aperturas pendientes de su tienda en la consola web; consultar sesiones abiertas (REPORT-FEAT-001) | Aprobar su propia apertura; abrir cajas desde la consola web |
| Sincronización del POS | Reenviar aperturas pendientes de sincronizar con el mismo identificador de operación | Generar un identificador nuevo en un reintento |

## Precondiciones

- El cajero inició sesión en el POS (AUTH-FEAT-001) y su sesión incluye la caja del dispositivo.
- El cajero tiene permiso para abrir caja.
- Para aprobar o rechazar: supervisor con sesión en la consola web y permiso de aprobación.

## Flujo principal

1. El cajero entra a "Apertura de caja" (VIEW-001); el sistema muestra la caja y el cajero de la sesión.
2. El cajero ingresa el monto inicial.
3. El cajero confirma "Abrir caja"; el sistema deshabilita la acción mientras procesa.
4. El sistema valida el monto (BR-CASH-001-02) y que la caja no esté ocupada (BR-CASH-001-01).
5. Monto hasta S/ 500.00: el sistema registra la sesión abierta con cajero, caja, fecha-hora y monto (REQ-007) y la confirma en menos de 2 s (REQ-006).
6. Monto mayor a S/ 500.00 (FLOW-001): el sistema registra la apertura "Pendiente de aprobación" (BR-CASH-001-06); el supervisor la ve en "Aprobación de aperturas de caja" (VIEW-002) y la aprueba (BR-CASH-001-07); la sesión queda abierta con el aprobador registrado.
7. El POS habilita la venta.

## Flujos alternativos

- A1 Sin conexión (paso 3), monto hasta S/ 500.00: el POS registra la apertura como "Pendiente de sincronizar", habilita la venta y la envía al recuperar la red (BR-CASH-001-05).
- A2 Monto inválido (paso 4): no se registra; mensaje en el campo.
- A3 Caja ocupada (paso 4), incluida la carrera entre dos dispositivos (Q-CASH-001-01): no se registra; mensaje "La caja ya tiene una sesión abierta".
- A4 Reintento o doble envío de la misma apertura: el sistema devuelve la apertura ya registrada sin crear otra (BR-CASH-001-04).
- A5 Apertura sin conexión rechazada al sincronizar: el POS la muestra como rechazada con su motivo; nunca se descarta en silencio.
- A6 Sesión del POS vencida: se pide iniciar sesión (AUTH-FEAT-001); la apertura pendiente se conserva.
- A7 Sin conexión y monto mayor a S/ 500.00 (CHG-001): el POS no permite la apertura y muestra "Para abrir con más de S/ 500.00 necesitas conexión" (BR-CASH-001-08).
- A8 El supervisor rechaza (CHG-001): la apertura queda rechazada, la caja queda libre y el POS muestra "El supervisor rechazó la apertura".
- A9 Dos supervisores deciden la misma apertura (CHG-001): cuenta la primera decisión; el segundo ve "Esta apertura ya fue decidida".
- A10 Un usuario con permisos de cajero y supervisor intenta aprobar su propia apertura (CHG-001): no se permite (BR-CASH-001-07).

## Estados

| Estado | Descripción | Transiciones permitidas |
|---|---|---|
| Pendiente de sincronizar | Registrada solo en el POS, sin confirmación del servidor | → Abierta (sincroniza), → Rechazada (el servidor la rechaza) |
| Pendiente de aprobación | Registrada en el servidor con monto mayor a S/ 500.00; ocupa la caja y no habilita ventas (CHG-001) | → Abierta (el supervisor aprueba), → Rechazada (el supervisor rechaza) |
| Abierta | Sesión vigente de la caja; habilita ventas | → Cerrada (fuera de alcance) |
| Rechazada | Rechazada al sincronizar o por el supervisor; conserva el motivo; la caja queda libre | ninguna (el cajero inicia una apertura nueva) |

## Reglas de negocio

| ID | Regla | Origen |
|---|---|---|
| BR-CASH-001-01 | Una caja tiene como máximo una sesión abierta o pendiente de aprobación a la vez | REQ-002, CHG-001 |
| BR-CASH-001-02 | El monto inicial está entre S/ 0.00 y S/ 5,000.00, en soles, con hasta 2 decimales | ASM-CASH-001-02 |
| BR-CASH-001-03 | La caja de la apertura es siempre la de la sesión del cajero; no se elige | ASM-CASH-001-03 |
| BR-CASH-001-04 | Reenviar la misma apertura (mismo identificador de operación) devuelve la apertura ya registrada y no crea otra | REQ-005, ASM-CASH-001-01 |
| BR-CASH-001-05 | Sin conexión, la apertura queda pendiente de sincronizar y habilita la venta en el POS | REQ-005, ASM-CASH-001-01 |
| BR-CASH-001-06 | Un monto inicial mayor a S/ 500.00 deja la apertura pendiente hasta que un supervisor la apruebe | REQ-008 |
| BR-CASH-001-07 | Solo un usuario con permiso de supervisor, distinto del cajero que solicitó, aprueba o rechaza una apertura pendiente | REQ-008 |
| BR-CASH-001-08 | Sin conexión no se permite solicitar una apertura mayor a S/ 500.00 | ASM-CASH-001-04 |

## Validaciones

| Campo | Regla | code | Mensaje |
|---|---|---|---|
| openingAmount | Obligatorio; entre 0.00 y 5000.00; máximo 2 decimales (BR-CASH-001-02) | OpeningAmount.Invalid | "Ingresa un monto entre S/ 0.00 y S/ 5,000.00 con hasta 2 decimales" |
| openingAmount (sin conexión) | Máximo S/ 500.00; validación local del POS (BR-CASH-001-08) | OpeningAmount.RequiresConnection | "Para abrir con más de S/ 500.00 necesitas conexión" |
| clientMutationId | Obligatorio; lo genera el POS una vez por intento de apertura | ClientMutationId.Required | "No se pudo identificar la operación. Vuelve a intentarlo." |

## Mensajes de error

| code | type | Mensaje al usuario | Cuándo |
|---|---|---|---|
| OpenCashSession.Validation | VALIDATION | Mensaje en cada campo (tabla Validaciones) | Algún campo no cumple su validación |
| CashSession.AlreadyOpen | CONFLICT | "La caja ya tiene una sesión abierta" | La caja está ocupada (BR-CASH-001-01) |
| CashSession.RegisterNotAssigned | CONFLICT | "Tu sesión no tiene una caja asignada. Vuelve a iniciar sesión." | La sesión del cajero no trae caja (BR-CASH-001-03) |
| CashSession.NotFound | NOT_FOUND | "La apertura no existe" | Aprobar o rechazar una apertura inexistente o de otra tienda |
| CashSession.NotPendingApproval | CONFLICT | "Esta apertura ya fue decidida" | La apertura ya no está pendiente (A9) |
| CashSession.SelfApprovalNotAllowed | CONFLICT | "No puedes aprobar tu propia apertura de caja" | BR-CASH-001-07 |
| Concurrency.Conflict | CONFLICT | "Esta apertura ya fue decidida" (tras recargar la lista) | Dos supervisores deciden a la vez (A9) |
| Auth.Forbidden | FORBIDDEN | "No tienes permiso para abrir caja" | Usuario sin permiso de apertura |
| Auth.Forbidden | FORBIDDEN | "No tienes permiso para aprobar aperturas de caja" | Usuario sin permiso de aprobación |
| Network.Unavailable | NETWORK | Sin mensaje de error: la apertura queda "Pendiente de sincronizar" | Sin conexión, monto hasta S/ 500.00 (BR-CASH-001-05) |
| Server.Failure | FAILURE | "No pudimos abrir la caja. Referencia: <correlationId>" | Error inesperado del servidor |

## Historias de usuario

### US-CASH-001-01 — Abrir la caja con monto inicial
Como cajero, quiero abrir la sesión de mi caja registrando el efectivo inicial, para empezar a vender con un saldo de arranque conocido.

#### AC-CASH-001-01 — Apertura con monto hasta el umbral deja la sesión abierta (BR-CASH-001-06)
Given un cajero con sesión iniciada en el POS de "Caja 01" y "Caja 01" sin sesión abierta
When abre la caja con un monto inicial de S/ 200.00
Then la sesión de "Caja 01" queda abierta con monto inicial S/ 200.00
And la apertura registra el cajero, la caja y la fecha-hora

#### AC-CASH-001-02 — Rechaza monto inicial fuera de rango (BR-CASH-001-02)
Given un cajero con sesión iniciada en el POS de "Caja 01" y "Caja 01" sin sesión abierta
When intenta abrir la caja con un monto inicial de S/ 5,000.01
Then la sesión no se abre
And el campo monto muestra "Ingresa un monto entre S/ 0.00 y S/ 5,000.00 con hasta 2 decimales"

#### AC-CASH-001-03 — Dos dispositivos abren la misma caja a la vez y solo una sesión queda abierta (BR-CASH-001-01)
Given "Caja 01" sin sesión abierta y dos dispositivos POS con sesión iniciada para "Caja 01"
When ambos dispositivos envían la apertura de "Caja 01" con S/ 100.00 al mismo tiempo
Then "Caja 01" queda con una sola sesión abierta
And el dispositivo cuya apertura no prosperó muestra "La caja ya tiene una sesión abierta"

### US-CASH-001-02 — Abrir la caja sin conexión
Como cajero, quiero abrir la caja aunque el POS no tenga conexión, para no detener la atención al cliente.

#### AC-CASH-001-04 — Apertura sin conexión hasta el umbral queda pendiente y se sincroniza (BR-CASH-001-05, BR-CASH-001-08)
Given un cajero con sesión iniciada en el POS de "Caja 02" y el POS sin conexión
When abre la caja con un monto inicial de S/ 150.00
Then la venta queda habilitada con la apertura "Pendiente de sincronizar"
And al recuperar la conexión la sesión de "Caja 02" queda abierta en el servidor con S/ 150.00

#### AC-CASH-001-05 — Reenviar la misma apertura no duplica la sesión (BR-CASH-001-04)
Given una apertura de "Caja 02" por S/ 150.00 ya registrada en el servidor
When el POS vuelve a enviar esa misma apertura por un reintento o un doble toque
Then "Caja 02" sigue con una sola sesión abierta
And el POS muestra la caja abierta sin mensaje de error

### US-CASH-001-03 — Aprobar aperturas con monto alto
Como supervisor, quiero aprobar o rechazar las aperturas con monto inicial mayor a S/ 500.00, para controlar el efectivo de arranque de las cajas de mi tienda.

#### AC-CASH-001-06 — Apertura mayor al umbral queda pendiente de aprobación (BR-CASH-001-06)
Given un cajero con sesión iniciada en el POS de "Caja 01", "Caja 01" sin sesión abierta y el POS con conexión
When abre la caja con un monto inicial de S/ 500.01
Then la apertura de "Caja 01" queda "Pendiente de aprobación" y la venta sigue deshabilitada
And la apertura aparece en "Aprobación de aperturas de caja" para los supervisores de la tienda

#### AC-CASH-001-07 — Supervisor aprueba la apertura pendiente (BR-CASH-001-07)
Given una apertura pendiente de "Caja 01" por S/ 800.00 solicitada por "cajero01"
When el supervisor "supervisor01" la aprueba
Then la sesión de "Caja 01" queda abierta con S/ 800.00 y aprobada por "supervisor01"
And el POS de "Caja 01" habilita la venta

#### AC-CASH-001-08 — Supervisor rechaza la apertura pendiente
Given una apertura pendiente de "Caja 01" por S/ 800.00 solicitada por "cajero01"
When el supervisor "supervisor01" la rechaza
Then la apertura queda rechazada y "Caja 01" queda sin sesión
And el POS muestra "El supervisor rechazó la apertura" y permite una nueva apertura

#### AC-CASH-001-09 — Nadie aprueba su propia apertura (BR-CASH-001-07)
Given una apertura pendiente de "Caja 01" solicitada por "mixto01", usuario con permisos de cajero y de supervisor
When "mixto01" intenta aprobarla
Then la apertura sigue pendiente
And se muestra el mensaje "No puedes aprobar tu propia apertura de caja"

## Dependencias y features relacionados

- Depende de: AUTH-FEAT-001 — la caja y los permisos del cajero llegan en su sesión.
- Relacionado: REPORT-FEAT-001 — consume el hecho "sesión abierta"; por CHG-001 recibe un cambio inducido (una sesión aprobada llega con la hora de aprobación).

## Vistas de referencia

- VIEW-001 — Apertura de caja — flujo principal y alternativos A1–A7.
- VIEW-002 — Aprobación de aperturas de caja — US-CASH-001-03 y alternativos A8–A10 (FLOW-001).

## Suposiciones

| ID | Suposición | Validar con | Estado |
|---|---|---|---|
| ASM-CASH-001-01 | Sin conexión, la apertura se registra en el POS y se envía después con el mismo identificador de operación | Jefatura de operaciones (ejemplo) | confirmada |
| ASM-CASH-001-02 | El monto inicial va de S/ 0.00 a S/ 5,000.00 con 2 decimales | Jefatura de caja (ejemplo) | confirmada |
| ASM-CASH-001-03 | Cada POS está asignado a una caja y la caja llega en la sesión del cajero | Operaciones (ejemplo) | confirmada |
| ASM-CASH-001-04 | La aprobación de supervisor requiere conexión: sin red, el POS no solicita aperturas mayores a S/ 500.00 | Jefatura de caja (ejemplo) | confirmada |

## Preguntas pendientes

| ID | Pregunta | Crítica | Estado | Respuesta |
|---|---|---|---|---|
| Q-CASH-001-01 | ¿Qué ocurre si dos dispositivos abren la misma caja al mismo tiempo? | si | resuelta | Solo queda abierta la primera apertura que confirma el servidor; el otro dispositivo ve "La caja ya tiene una sesión abierta" (jefatura de caja, en el ejemplo, 2026-08-14). Reflejado en AC-CASH-001-03 |
| Q-CASH-001-02 | ¿Se imprime un comprobante de apertura en la ticketera del POS? | no | abierta | |
