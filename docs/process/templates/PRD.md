# PRD — Market Real

<!-- Se copia a docs/product/PRD.md cuando llega el PRD real. Dueño: analista funcional.
     Declara los REQ-### (primera celda de las tablas de requisitos). Pide el número con: trace next-id REQ
     Un REQ no se renumera ni se borra: se marca Estado = retirado. Cambios sobre features ya en ready → CHG. -->

## Control del documento

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 0.1.0 | {{DATE}} | | Borrador inicial |

## Visión

<!-- Qué es el producto, para quién y qué resultado de negocio persigue (3–5 líneas). -->

## Usuarios y roles

| Rol | Descripción | Plataforma | Objetivos principales |
|---|---|---|---|

<!-- Plataforma ∈ web | POS | preventa | backoffice. Los permisos detallados viven en cada FEATURE.md. -->

## Alcance

<!-- Qué incluye esta versión del producto y qué queda explícitamente fuera. -->

## Requisitos funcionales

| ID | Requisito | Prioridad | Origen | Vistas | Módulo | Estado |
|---|---|---|---|---|---|---|

<!-- Prioridad ∈ must|should|could|wont · Origen = maquetado, entrevista, normativa (p. ej. SUNAT), documento
     Vistas = VIEW-### separadas por coma · Módulo = código de modules.yaml o "por decidir" · Estado ∈ propuesto|confirmado|retirado
     Ejemplo de fila (sin sangría, número real con trace next-id):
    | REQ-### | El cajero registra el pago total o parcial de una venta | must | Maquetado cobro + entrevista caja | VIEW-### | por decidir | propuesto | -->

## Requisitos no funcionales

| ID | Requisito | Categoría | Métrica objetivo | Origen | Estado |
|---|---|---|---|---|---|

<!-- Categoría ∈ rendimiento|disponibilidad|offline|seguridad|auditoría|accesibilidad|compatibilidad|normativa
     Métrica verificable: "confirmación de venta < 2 s en POS", no "rápido". -->

## Catálogo de reglas de negocio

| Regla | Requisitos | Vistas | Confirmada por | Estado |
|---|---|---|---|---|

<!-- Reglas transversales detectadas a nivel producto. No llevan ID aquí: al crear el feature se declaran como BR en FEATURE.md con Origen = REQ.
     Estado ∈ por confirmar|confirmada. Lo no confirmado nunca se implementa como regla. -->

## Integraciones externas

| Sistema | Propósito | Dirección | Requisitos | Restricciones conocidas |
|---|---|---|---|---|

<!-- SUNAT (comprobantes electrónicos), OCR, GPS, pasarelas u otras. Dirección ∈ entrada|salida|ambas. -->

## Glosario

| Término | Definición |
|---|---|

## Preguntas abiertas

| ID | Pregunta | Crítica | Estado | Respuesta |
|---|---|---|---|---|

<!-- Declara Q-PRD-### (trace next-id Q-PRD). Cita en la pregunta los REQ o VIEW afectados.
     Crítica ∈ si|no · Estado ∈ abierta|resuelta (valores ASCII, como en el esquema). Ejemplo:
    | Q-PRD-### | ¿El pago parcial aplica a ventas a crédito? (REQ-###, VIEW-###) | si | abierta | | -->
     Al crear un feature, las preguntas que le aplican se trasladan a su tabla de Preguntas pendientes como Q-…. -->
