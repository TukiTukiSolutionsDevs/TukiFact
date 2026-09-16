---
id: AUTH-FEAT-001
title: "Iniciar sesión en POS"
module: AUTH
status: released
spec_version: 1.0.0
owners: { analyst: "analista (ejemplo)", architect: "arquitecto (ejemplo)", developer: "dev-backend y dev-mobile (ejemplo)", qa: "qa (ejemplo)" }
prd_requirements: [REQ-003]
views: []
depends_on: []
related: [CASH-FEAT-001]
repos: [backend, mobile]
blocked_reason: ""
blocked_from: ""
target_release: ""
released_in: REL-2026.09.1
updated: 2026-09-07
---

# AUTH-FEAT-001 — Iniciar sesión en POS

> **Ejemplo ilustrativo y mínimo.** Existe solo como dependencia de CASH-FEAT-001 dentro del ejemplo `pos-cash-opening`. En Market Real el contrato de autenticación sigue pendiente de decisión (`AGENTS.md`); aquí se resuelve con la suposición ASM-AUTH-001-01, confirmada solo en el ejemplo.

## Objetivo

El cajero inicia sesión en el POS y queda asociado a la caja del dispositivo, para poder operarla.

## Problema

Sin sesión identificada no se puede atribuir la apertura de caja a un cajero ni a una caja (REQ-003).

## Alcance

- Inicio de sesión del cajero en el POS con usuario y contraseña.
- La sesión incluye la caja asignada al dispositivo y los permisos del cajero.

## Fuera de alcance

- Renovación de sesión, cierre de sesión y recuperación de contraseña.
- Inicio de sesión en la consola web (base de autenticación existente).

## Actores, roles y permisos

| Rol | Puede | No puede |
|---|---|---|
| Cajero | Iniciar sesión en el POS asignado a su tienda | Elegir la caja del dispositivo |

## Precondiciones

- El dispositivo POS está registrado y asignado a una caja.
- El cajero tiene un usuario activo.

## Flujo principal

1. El cajero ingresa usuario y contraseña en el POS.
2. El sistema valida las credenciales.
3. El sistema abre la sesión con la caja del dispositivo y los permisos del cajero.
4. El POS muestra la pantalla de apertura de caja (CASH-FEAT-001).

## Flujos alternativos

- A1 Credenciales inválidas: no se abre sesión; mensaje genérico sin indicar qué dato falló.
- A2 Sin conexión: no se permite iniciar sesión por primera vez en el dispositivo.

## Estados

No aplica — la sesión del POS no tiene estados de negocio en este feature.

## Reglas de negocio

| ID | Regla | Origen |
|---|---|---|
| BR-AUTH-001-01 | La sesión del cajero incluye la caja asignada al dispositivo POS | ASM-AUTH-001-01 |

## Validaciones

| Campo | Regla | code | Mensaje |
|---|---|---|---|
| userName | Obligatorio | UserName.Required | "Ingresa tu usuario" |
| password | Obligatorio | Password.Required | "Ingresa tu contraseña" |

## Mensajes de error

| code | type | Mensaje al usuario | Cuándo |
|---|---|---|---|
| StartPosSession.Validation | VALIDATION | Mensajes por campo | Falta usuario o contraseña |
| Auth.InvalidCredentials | VALIDATION | "Usuario o contraseña incorrectos" | Credenciales inválidas |

## Historias de usuario

### US-AUTH-001-01 — Iniciar sesión en el POS
Como cajero, quiero iniciar sesión en el POS con mi usuario y contraseña, para operar la caja asignada al dispositivo.

#### AC-AUTH-001-01 — Credenciales válidas abren la sesión con la caja del dispositivo
Given un POS asignado a "Caja 01" y el usuario "cajero01" activo
When "cajero01" inicia sesión con su contraseña correcta
Then la sesión queda iniciada
And la sesión indica la caja "Caja 01"

#### AC-AUTH-001-02 — Credenciales inválidas no abren sesión
Given un POS asignado a "Caja 01" y el usuario "cajero01" activo
When "cajero01" inicia sesión con una contraseña incorrecta
Then la sesión no se inicia
And se muestra el mensaje "Usuario o contraseña incorrectos"

## Dependencias y features relacionados

- Relacionado: CASH-FEAT-001 — usa la caja de la sesión para abrirla.

## Vistas de referencia

No aplica — la pantalla de inicio de sesión no forma parte del maquetado de este ejemplo.

## Suposiciones

| ID | Suposición | Validar con | Estado |
|---|---|---|---|
| ASM-AUTH-001-01 | Cada POS está asignado a una sola caja y esa caja viaja en la sesión del cajero | Operaciones (ejemplo) | confirmada |

## Preguntas pendientes

| ID | Pregunta | Crítica | Estado | Respuesta |
|---|---|---|---|---|
