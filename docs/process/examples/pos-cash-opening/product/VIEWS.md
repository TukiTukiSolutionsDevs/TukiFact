# Inventario de vistas y flujos — Market Real (ejemplo)

> **Ejemplo ilustrativo.** Este inventario pertenece al ejemplo `pos-cash-opening` y no describe el maquetado real de Market Real. Los enlaces de maquetado son ficticios.

## Control del documento

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 0.1.0 | 2026-08-10 | Analista funcional (ejemplo) | Inventario inicial desde el maquetado de caja y reportes |

## Vistas

| ID | Nombre | Plataforma | Tipo | Maquetado | Roles | Módulo | Estado |
|---|---|---|---|---|---|---|---|
| VIEW-001 | Apertura de caja | POS | pantalla | https://www.figma.com/design/EJEMPLO-caja?node-id=10-1 | cajero | CASH | analizada |
| VIEW-002 | Aprobación de aperturas de caja | web | panel | https://www.figma.com/design/EJEMPLO-caja?node-id=10-7 | supervisor | CASH | analizada |
| VIEW-003 | Reporte de sesiones de caja | web | pantalla | https://www.figma.com/design/EJEMPLO-reportes?node-id=4-2 | supervisor | REPORT | analizada |

## Flujos

| ID | Nombre | Actor | Vistas en orden | Maquetado | Estado |
|---|---|---|---|---|---|
| FLOW-001 | Apertura de caja con aprobación de supervisor | cajero, supervisor | VIEW-001 → VIEW-002 → VIEW-001 | https://www.figma.com/proto/EJEMPLO-caja?node-id=10-1 | analizado |

## Maquetado sin clasificar

Ninguno. El frame "Cierre de caja" del mismo archivo queda fuera de este ejemplo y no recibe ID hasta que se analice.
