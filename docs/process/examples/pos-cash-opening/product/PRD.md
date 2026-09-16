# PRD — Market Real (ejemplo)

> **Ejemplo ilustrativo.** Este PRD es parte del ejemplo `pos-cash-opening`. Los requisitos, umbrales y respuestas son supuestos del ejemplo para mostrar el mecanismo `REQ` / `Q-PRD` / `ASM`; no son decisiones reales de Market Real.

## Control del documento

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 0.2.0 | 2026-09-15 | Analista funcional (ejemplo) | Q-PRD-002 resuelta; nuevo REQ-008 por CHG-001 |
| 0.1.0 | 2026-08-10 | Analista funcional (ejemplo) | Borrador inicial: apertura de caja, inicio de sesión POS y reporte de sesiones |

## Visión

Market Real opera tiendas minoristas en Perú con puntos de venta Android. Este alcance cubre el inicio del turno de caja: el cajero inicia sesión en el POS, abre su caja con el efectivo inicial (también sin conexión) y el supervisor controla esas aperturas desde la consola web.

## Usuarios y roles

| Rol | Descripción | Plataforma | Objetivos principales |
|---|---|---|---|
| Cajero | Atiende una caja asignada a su dispositivo | POS | Iniciar el turno rápido y sin errores de efectivo |
| Supervisor | Responsable de las cajas de una tienda | web | Controlar el efectivo inicial y las aperturas |

## Alcance

- Incluye: inicio de sesión en POS, apertura de sesión de caja (con y sin conexión), aprobación de aperturas con monto alto, reporte de sesiones abiertas.
- Fuera: cierre y arqueo de caja, ventas, comprobantes SUNAT.

## Requisitos funcionales

| ID | Requisito | Prioridad | Origen | Vistas | Módulo | Estado |
|---|---|---|---|---|---|---|
| REQ-001 | El cajero abre una sesión de caja registrando el monto inicial en efectivo | must | Maquetado de caja + entrevista con jefatura de caja (ejemplo) | VIEW-001 | CASH | confirmado |
| REQ-002 | Una caja tiene como máximo una sesión abierta a la vez | must | Entrevista con jefatura de caja (ejemplo) | VIEW-001 | CASH | confirmado |
| REQ-003 | El cajero inicia sesión en el POS y queda asociado a la caja del dispositivo | must | Entrevista con operaciones (ejemplo) | — | AUTH | confirmado |
| REQ-004 | El supervisor consulta las sesiones de caja abiertas por fecha y caja | should | Maquetado de reportes (ejemplo) | VIEW-003 | REPORT | confirmado |
| REQ-008 | Una apertura con monto inicial mayor a S/ 500.00 requiere la aprobación de un supervisor distinto del cajero antes de habilitar la venta | must | Q-PRD-002 y CHG-001 (jefatura de caja, ejemplo) | VIEW-001, VIEW-002 | CASH | confirmado |

## Requisitos no funcionales

| ID | Requisito | Categoría | Métrica objetivo | Origen | Estado |
|---|---|---|---|---|---|
| REQ-005 | La apertura de caja funciona sin conexión y se sincroniza al recuperar la red | offline | 100 % de aperturas offline sincronizadas sin sesiones duplicadas | Q-PRD-001 | confirmado |
| REQ-006 | Confirmación de apertura rápida en el POS | rendimiento | < 2 s p95 con conexión 4G en el POS de referencia | Entrevista con operaciones (ejemplo) | confirmado |
| REQ-007 | Toda apertura queda auditada | auditoría | 100 % de aperturas con usuario, caja, fecha-hora y monto (y aprobador, si aplica) | Control interno (ejemplo) | confirmado |

## Catálogo de reglas de negocio

| Regla | Requisitos | Vistas | Confirmada por | Estado |
|---|---|---|---|---|
| Una sola sesión abierta por caja | REQ-002 | VIEW-001 | Jefatura de caja (ejemplo) | confirmada |
| Límites del monto inicial | REQ-001 | VIEW-001 | Jefatura de caja (ejemplo), registrada como suposición en el feature | confirmada |
| Umbral de aprobación: montos mayores a S/ 500.00 (límite exclusivo) | REQ-008 | VIEW-002 | Jefatura de caja (ejemplo), Q-PRD-002 | confirmada |

## Integraciones externas

| Sistema | Propósito | Dirección | Requisitos | Restricciones conocidas |
|---|---|---|---|---|
| — | Sin integraciones externas en este alcance | — | — | SUNAT queda fuera: la apertura no emite comprobante |

## Glosario

| Término | Definición |
|---|---|
| Sesión de caja | Periodo entre la apertura y el cierre de una caja por un cajero |
| Monto inicial | Efectivo con el que se abre la sesión, en soles (PEN) |
| Umbral de aprobación | Monto inicial a partir del cual un supervisor debe aprobar la apertura |

## Preguntas abiertas

| ID | Pregunta | Crítica | Estado | Respuesta |
|---|---|---|---|---|
| Q-PRD-001 | ¿La apertura de caja debe poder hacerse sin conexión? (REQ-001, VIEW-001) | si | resuelta | Sí (confirmado en el ejemplo el 2026-08-10): se registra en el POS y se sincroniza al volver la red; da origen a REQ-005 |
| Q-PRD-002 | ¿Cuál es el umbral de monto inicial que exige aprobación de supervisor y el límite es inclusivo? (VIEW-002) | si | resuelta | S/ 500.00; requiere aprobación todo monto **mayor** a S/ 500.00 (confirmado en el ejemplo el 2026-09-14). Da origen a REQ-008 y a CHG-001 |
