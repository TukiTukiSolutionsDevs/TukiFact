# Técnicas de diseño de pruebas

Cada caso resultante es una fila `TEST-*` en `QA-PLAN.md` con su `Tipo` (§4.4). Ejemplos del dominio POS; los valores reales salen de `FEATURE.md` (`BR-*`), nunca se inventan.

## Particiones de equivalencia

Dividir cada entrada en clases que el sistema trata igual; un caso por clase válida y por clase inválida.

Pago en efectivo sobre una venta de S/ 45.50:

| Clase | Ejemplo | Tipo |
|---|---|---|
| Monto ≥ total | S/ 50.00 | positivo |
| 0 < monto < total (sin pago parcial) | S/ 40.00 | negativo |
| Monto ≤ 0 | S/ 0.00, S/ -5.00 | negativo |
| No numérico / vacío | `abc`, vacío | negativo |

## Valores límite

Probar justo en, debajo y encima de cada frontera.

| Frontera | Casos |
|---|---|
| Monto = total (S/ 45.50) | 45.49, 45.50, 45.51 |
| Tope de descuento de cajero 10 % | 9.99 %, 10 %, 10.01 % |
| Máximo de ítems por venta (si hay `BR`) | máx − 1, máx, máx + 1 |
| Precisión monetaria | 2 decimales vs 3 decimales (redondeo) |

## Tablas de decisión

Cuando el resultado depende de combinar condiciones. Una columna = un caso.

Anulación de venta:

| Condición / Caso | 1 | 2 | 3 | 4 |
|---|---|---|---|---|
| Venta del turno abierto | sí | sí | no | no |
| Rol supervisor | sí | no | sí | no |
| **Resultado** | anula | pide supervisor | anula con motivo | rechaza |

Colapsar columnas con igual resultado solo si las condiciones son realmente irrelevantes.

## Transiciones de estado

Modelar estados y eventos; probar cada transición válida y al menos una inválida por estado.

Turno de caja:

| Desde | Evento | Hacia | Tipo |
|---|---|---|---|
| cerrado | abrir con fondo inicial | abierto | positivo |
| abierto | registrar venta | abierto | positivo |
| abierto | cerrar con arqueo | cerrado | positivo |
| cerrado | registrar venta | — (rechazo) | negativo |
| abierto | abrir otra vez (mismo cajero) | — (rechazo) | negativo |
| abierto | cierre concurrente desde dos dispositivos | un solo cierre | concurrencia |

## Pairwise (todos los pares)

Para muchas variables con pocas opciones cada una: cubrir cada par de valores al menos una vez en lugar del producto completo.

Variables: medio de pago {efectivo, tarjeta, yape}, comprobante {boleta, factura}, app {POS, preventa}, red {online, offline}. Producto completo = 24; pairwise ≈ 6–8 casos. Agregar a mano las combinaciones de alto riesgo (factura + offline) aunque el algoritmo no las elija.

## Matriz de permisos

Rol × acción × resultado esperado. Probar cada celda `denegado` también por API directa, no solo ocultando el botón.

| Acción | Cajero | Supervisor | Vendedor preventa | Admin |
|---|---|---|---|---|
| Abrir caja | permitido | permitido | denegado | permitido |
| Registrar pago | permitido | permitido | denegado | denegado |
| Anular pago | pide supervisor | permitido | denegado | permitido |
| Ver arqueo de otros | denegado | permitido | denegado | permitido |

Los roles y acciones reales salen de `Actores, roles y permisos` de `FEATURE.md`.

## Otros tipos a considerar

| Tipo | Pregunta guía (POS) |
|---|---|
| error | ¿Qué pasa si SUNAT, la impresora o la red fallan a mitad del cobro? |
| concurrencia | ¿Dos cajeros cobran la misma venta o descuentan el mismo stock? |
| seguridad | ¿Se puede cobrar con un token de otro rol o manipular el monto en el request? |
| rendimiento | ¿Búsqueda de producto y cobro dentro del tiempo del `FEATURE.md` con el volumen esperado? |
| accesibilidad | ¿Contraste, tamaño táctil y lector de pantalla en la vista de cobro? |
| compatibilidad | ¿Versiones de Android y navegadores soportados? |
| recuperacion | ¿La app se reinicia en medio del pago y no duplica ni pierde la venta? |
| migracion | ¿Ventas y turnos existentes siguen legibles tras el cambio de esquema? |
| regresion | ¿Los TEST de features que consumen el mismo contrato o evento siguen pasando? |

## Selección de nivel

Probar cada regla en el nivel más bajo que la cubra (dominio/unit), y los flujos críticos de punta a punta (e2e) solo una vez por flujo. Detalle por plataforma: [../../backend-testing/SKILL.md](../../backend-testing/SKILL.md), [../../frontend-testing/SKILL.md](../../frontend-testing/SKILL.md), [../../mobile-testing/SKILL.md](../../mobile-testing/SKILL.md).
