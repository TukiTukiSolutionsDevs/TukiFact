# Reglas POS (UI)

Fuente: `design-system/rules/pos-rules.json`.

- Una acción primaria por sección; altura mínima 52; alto contraste.
- Dinero: números tabulares; moneda siempre visible.
- Estado de dispositivo siempre visible; nunca solo color.
- Scanner: feedback visual **y** sonoro.
- Balanza: peso estable antes de confirmar.
- Errores críticos: persistentes hasta ACK.
- Pago `unknown`: reconciliación (negocio; ver `mobile-architecture` POS).
- Carrito: no commitea inventario al agregar (negocio + UI no debe sugerir lo contrario).

## Generación

- Android: los valores numéricos se generan en `MrPosTokens` (p. ej. `primaryActionMinimumHeight`); `MrThemeRulesTest` verifica que no sea menor que el touch target. Tipografías tabulares: `MrTheme.typography.moneyLarge` / `weightLarge`.
- Web: el generador lee solo `tokens/`; `pos-rules.json` no produce CSS.
