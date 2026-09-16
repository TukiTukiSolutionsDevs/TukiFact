# Checklist: análisis funcional de un feature

Cada casilla termina en una sección de `FEATURE.md`, una `BR-*`, una `Q-*` o una `ASM-*`. "No aplica" se escribe con motivo; no se deja en blanco.

## Alcance y origen

- [ ] Cada comportamiento tiene `REQ-###` o `ASM-*`; nada sale solo del maquetado.
- [ ] `Fuera de alcance` explícito (lo que el maquetado sugiere pero no se construye ahora).
- [ ] `views` y `prd_requirements` completos y existentes en `VIEWS.md` / `PRD.md`.

## Reglas de negocio

- [ ] Cálculos (montos, redondeo, impuestos, descuentos, vuelto) con fórmula y moneda.
- [ ] Límites (máximos, mínimos, topes por rol, por caja, por día).
- [ ] Unicidad e idempotencia percibida (doble clic, reintento, reenvío).
- [ ] Reglas temporales (turno de caja, cierre del día, zona horaria, vigencia).

## Actores, roles y permisos

- [ ] Quién ve, crea, edita, anula, aprueba; qué ve cada rol en la misma vista.
- [ ] Acción sin permiso: oculta, deshabilitada o error (qué mensaje).
- [ ] Aprobación de supervisor o doble control en operaciones sensibles (anulaciones, devoluciones, descuentos).

## Validaciones y mensajes

- [ ] Por campo: obligatorio, formato, rango, longitud, dependencia entre campos.
- [ ] Validación de servidor que la UI no puede anticipar (stock, saldo, estado vigente).
- [ ] `Mensajes de error` con texto funcional por caso (el `code` técnico lo define arquitectura).

## Estados

- [ ] Estados de la entidad y transiciones permitidas y prohibidas.
- [ ] Quién dispara cada transición y qué efecto tiene (stock, caja, notificación).
- [ ] Qué pasa con registros en estado intermedio al cancelar o fallar.

## Flujos alternativos y errores

- [ ] Cancelación a mitad del flujo, datos incompletos, sesión expirada.
- [ ] Sin conexión o dispositivo sin red (POS/preventa): qué se permite. Si depende del protocolo offline aún no decidido → `Q-*` crítica.
- [ ] Falla de hardware (impresora, lector, balanza) y de integraciones.
- [ ] Concurrencia de negocio: dos usuarios sobre el mismo registro, misma caja, mismo stock.

## Procesos en segundo plano y comunicación entre módulos

- [ ] Efectos diferidos (sincronización, recalculos, cierres automáticos, reintentos).
- [ ] Otros módulos que deben enterarse del hecho (stock, caja, reportes, notificaciones) → `related`/`depends_on`.
- [ ] Features existentes cuyo comportamiento cambia → `related` y aviso a arquitectura.

## Integraciones externas

- [ ] SUNAT (comprobantes, plazos, rechazos, contingencia).
- [ ] OCR (confianza mínima, corrección manual).
- [ ] GPS (precisión, permiso denegado, sin señal).
- [ ] Qué ve el usuario si la integración tarda o falla.

## Eventos y notificaciones

- [ ] Quién recibe qué aviso, por qué canal y cuándo.
- [ ] Qué hechos de negocio se deben poder consultar después (historial).

## Seguridad, auditoría y datos

- [ ] Datos personales o sensibles mostrados, exportados o impresos.
- [ ] Qué acciones quedan auditadas (quién, cuándo, antes/después).
- [ ] Datos existentes afectados: migración, valores por defecto, registros históricos.
- [ ] Retención o borrado exigido por negocio o ley.

## Rendimiento y operación

- [ ] Volúmenes esperados (ítems por venta, registros por listado, usuarios simultáneos).
- [ ] Tiempos aceptables percibidos en caja (p. ej. cobro, búsqueda de producto).
- [ ] Reportes o listados: filtros, orden, paginación, exportación.

## Cierre

- [ ] Cada historia con ≥1 AC; cada AC con Given/When/Then.
- [ ] `Q-*` críticas identificadas y asignadas; ninguna regla sin `Origen`.
- [ ] `trace check` sin errores.
