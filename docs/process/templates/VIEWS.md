# Inventario de vistas y flujos — Market Real

<!-- Se copia a docs/product/VIEWS.md. Dueño: analista funcional.
     Declara VIEW-### y FLOW-### (primera celda de cada fila). Pide el número con: trace next-id VIEW | trace next-id FLOW
     Una vista = pantalla, modal o panel con propósito propio. Un flujo = recorrido de varias vistas para completar una tarea.
     Un ID no se renumera ni se borra: Estado = retirada. -->

## Control del documento

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 0.1.0 | {{DATE}} | | Borrador inicial |

## Vistas

| ID | Nombre | Plataforma | Tipo | Maquetado | Roles | Módulo | Estado |
|---|---|---|---|---|---|---|---|

<!-- Plataforma ∈ web|POS|preventa · Tipo ∈ pantalla|modal|panel|impresión
     Maquetado = enlace al frame concreto (Figma u otra herramienta) o ruta a la imagen
     Módulo = código de modules.yaml o "por decidir" · Estado ∈ identificada|analizada|retirada
     Ejemplo de fila (sin sangría, número real con trace next-id):
    | VIEW-### | Cobro de venta | POS | pantalla | https://www.figma.com/design/<archivo>?node-id=<nodo> | cajero | por decidir | identificada | -->

## Flujos

| ID | Nombre | Actor | Vistas en orden | Maquetado | Estado |
|---|---|---|---|---|---|

<!-- Vistas en orden = VIEW-### → VIEW-### · Estado ∈ identificado|analizado|retirado
    | FLOW-### | Venta con pago parcial | cajero | VIEW-### → VIEW-### → VIEW-### | <enlace al prototipo> | identificado | -->

## Maquetado sin clasificar

<!-- Pantallas recibidas que aún no se sabe si son vista nueva, variante o estado de otra vista. Resolver antes de crear features. -->
