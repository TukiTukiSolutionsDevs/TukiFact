# Análisis funcional — Market Real (ejemplo)

> **Ejemplo ilustrativo.** Las reglas de negocio reales de Market Real no están definidas. Todo lo marcado como "Inferido" sale de un maquetado ficticio y todo lo "Por confirmar" se resolvió dentro del ejemplo mediante `Q-PRD-…`, `Q-…` o `ASM-…`; no son decisiones reales del producto.

## Control del documento

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 0.1.0 | 2026-08-10 | Analista funcional (ejemplo) | Análisis de VIEW-001, VIEW-002 y VIEW-003 |

## Resumen de cobertura

| Vista | Analizada | Pendientes críticos | Requisitos |
|---|---|---|---|
| VIEW-001 | si | 0 (el comportamiento offline se resolvió con Q-PRD-001) | REQ-001, REQ-002, REQ-005, REQ-006, REQ-007 |
| VIEW-002 | si | 0 (Q-PRD-002 resuelta el 2026-09-14) | REQ-008 |
| VIEW-003 | si | 0 | REQ-004 |

## Vista: Apertura de caja (VIEW-001)

### Propósito

El cajero registra el efectivo con el que inicia su turno en la caja asignada al dispositivo POS. Primer paso de FLOW-001; sin sesión abierta no se puede vender.

### Campos

| Campo | Tipo | Obligatorio | Formato / límites | Origen del dato | Editable por |
|---|---|---|---|---|---|
| Caja | texto (solo lectura) | si | nombre de la caja | sesión del cajero en el POS | nadie |
| Cajero | texto (solo lectura) | si | nombre completo | sesión del cajero en el POS | nadie |
| Monto inicial | moneda | si | S/ con 2 decimales; el maquetado no muestra límites | ingresado por el cajero | cajero |
| Estado de sincronización | etiqueta | no | "Sincronizada" / "Pendiente de sincronizar" | dispositivo | nadie |

### Acciones

| Acción | Disparador | Resultado esperado | Confirmación | Rol |
|---|---|---|---|---|
| Abrir caja | botón "Abrir caja" | la caja queda abierta y se habilita la venta | no | cajero |
| Reintentar sincronización | aviso "Pendiente de sincronizar" | se reenvía la apertura pendiente | no | cajero |

### Estados

Vacío (sin monto), editando, enviando (botón deshabilitado), abierta, pendiente de sincronizar (sin conexión), pendiente de aprobación (monto mayor a S/ 500.00, REQ-008), error con mensaje.

### Reglas visibles

- El botón "Abrir caja" solo se habilita con un monto ingresado.
- El maquetado muestra un aviso "Requiere aprobación de supervisor" junto a montos altos, sin indicar el umbral.

### Errores

- Visibles en el maquetado: "Monto inválido".
- No aparecen pero deben existir: caja ya abierta (otro dispositivo), sin conexión, sin permiso para abrir caja.

### Permisos

Solo el cajero autenticado en el POS ve la vista; la caja no se elige, viene de su sesión.

### Inferido vs por confirmar

| Elemento | Inferido del maquetado | Por confirmar | Crítico |
|---|---|---|---|
| Límites del monto inicial | 2 decimales | Mínimo y máximo | si |
| Operación sin conexión | aviso "Pendiente de sincronizar" | ¿Se permite abrir sin red? (Q-PRD-001) | si |
| Aperturas simultáneas | — | ¿Qué pasa si dos dispositivos abren la misma caja? | si |
| Doble toque en "Abrir caja" | botón deshabilitado al enviar | Que nunca se creen dos sesiones | si |
| Comprobante impreso | — | ¿Se imprime un ticket de apertura? | no |

## Vista: Aprobación de aperturas de caja (VIEW-002)

### Propósito

El supervisor revisa en la consola web las aperturas cuyo monto inicial exige aprobación y las aprueba o rechaza. Paso intermedio de FLOW-001.

### Campos

| Campo | Tipo | Obligatorio | Formato / límites | Origen del dato | Editable por |
|---|---|---|---|---|---|
| Caja | texto | si | nombre de la caja | apertura pendiente | nadie |
| Cajero | texto | si | nombre completo | apertura pendiente | nadie |
| Monto inicial | moneda | si | S/ con 2 decimales | apertura pendiente | nadie |
| Solicitada | fecha-hora | si | hora de Lima | apertura pendiente | nadie |

### Acciones

| Acción | Disparador | Resultado esperado | Confirmación | Rol |
|---|---|---|---|---|
| Aprobar | botón "Aprobar" | la sesión queda abierta y el POS lo refleja | si | supervisor |
| Rechazar | botón "Rechazar" | la apertura se rechaza y la caja queda libre | si | supervisor |

### Estados

Vacío (sin pendientes), con pendientes, procesando, error, ya decidida por otro supervisor.

### Reglas visibles

- Solo se listan aperturas pendientes de la tienda del supervisor.

### Errores

- No aparecen pero deben existir: la apertura ya fue decidida por otro supervisor; sin permiso; el supervisor intenta aprobar su propia apertura.

### Permisos

Visible solo para supervisores. El cajero no ve la vista.

### Inferido vs por confirmar

| Elemento | Inferido del maquetado | Por confirmar | Crítico |
|---|---|---|---|
| Umbral que exige aprobación | aviso de "monto alto" | Valor del umbral y si el límite es inclusivo (Q-PRD-002) | si |
| Aprobación sin conexión | — | ¿Se puede aprobar si el POS está offline? | si |
| Quién puede aprobar | rol supervisor | ¿Puede aprobar quien abrió la caja si también es supervisor? | si |

## Vista: Reporte de sesiones de caja (VIEW-003)

### Propósito

El supervisor consulta las sesiones de caja abiertas en una fecha, con su monto inicial, para controlar el efectivo de arranque de cada caja.

### Campos

| Campo | Tipo | Obligatorio | Formato / límites | Origen del dato | Editable por |
|---|---|---|---|---|---|
| Fecha | fecha | si | por defecto hoy (hora de Lima) | filtro | supervisor |
| Caja | lista | no | todas por defecto | filtro | supervisor |
| Sesiones | tabla | — | caja, hora de apertura, monto inicial | módulo de caja | nadie |

### Acciones

| Acción | Disparador | Resultado esperado | Confirmación | Rol |
|---|---|---|---|---|
| Consultar | cambio de filtros | la tabla muestra las sesiones que cumplen los filtros | no | supervisor |

### Estados

Cargando, sin resultados, con resultados, error.

### Reglas visibles

- Orden por hora de apertura, más reciente primero.

### Errores

- No aparecen pero deben existir: sin permiso; error genérico con referencia de soporte.

### Permisos

Solo supervisores con permiso de reportes.

### Inferido vs por confirmar

| Elemento | Inferido del maquetado | Por confirmar | Crítico |
|---|---|---|---|
| Nombre del cajero | columna "Cajero" en el maquetado | Requiere datos de usuarios de otro módulo; se excluye de la primera versión | no |
| Retraso aceptable | — | ¿El reporte debe ser en tiempo real? | no |
| Exportación | — | ¿Se exporta a Excel? | no |
