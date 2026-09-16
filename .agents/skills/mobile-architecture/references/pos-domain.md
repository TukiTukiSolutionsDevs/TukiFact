# POS — reglas de negocio

- No venta sin turno abierto.
- Agregar al carrito no descuenta stock definitivo.
- Payment `Unknown` exige reconciliación.
- Android no genera fiscalmente ni llama al facturador.
- Customer display es proyección de `SaleState`.
- Hardware no contiene reglas de negocio.
- Venta/pago al backend con idempotencia (`client_mutation_id`).
- El ticket físico no es fuente fiscal de verdad.

UI POS: `design-system/rules/pos-rules.json` (acción primaria alta, peso estable antes de confirmar, error crítico persistente).
