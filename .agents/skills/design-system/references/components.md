# Componentes

Contrato JSON (`design-system/schemas/component.schema.json`): `component`, `variants`, `states`, `rules`, tamaños con tokens. Rutas relativas a la raíz del repo.

## Estado de los renderers

Ningún `Mr*` está implementado todavía, ni en Angular ni en Compose. Hoy solo existen tokens generados (web y Android) y `MrTheme` (Android). Construir los renderers `Mr*` es el siguiente paso; cada uno sigue [checklists/new-component.md](../checklists/new-component.md).

- Angular: componente standalone en `MarketjoyaFront/src/ui/`, selector `mr-*`, estilos con `var(--mr-*)`.
- Compose: en `MarketjoyaMobile/core/designsystem`, dentro de `MrTheme`.

## Primitivos (`design-system/components/`)

`button.json` (`MrButton`), `text-field.json`, `status-chip.json`, `card.json`, `dialog.json`.

## De negocio (`design-system/business-components/`)

`money-display`, `weight-display`, `sale-total`, `receipt-preview`, `payment-method-card`, `device-status`.

Las pantallas POS/preventa/web de dinero y peso **no** arman un Text + color local: usan el business component.

Si falta variante: se añade al JSON y luego al renderer. No se crea `MyPrimaryButton` en la feature.
