# Compose UI

Pocas Screens: las de contrato (totales, peso, error crítico, permiso de botón). Source set `androidTest`; dependencias vía `marketjoya.android.compose`.

- `createComposeRule` para Screen aislada. `createAndroidComposeRule` solo navegación / deep link.
- Lookup: `testTag` estable (kebab-case, ej. `pos-root`). Texto i18n es frágil.
- Envolver en `MrTheme`; no asertar color HEX.
- `waitUntil` con timeout, no `sleep`. Reloj: `mainClock.autoAdvance = false` si la animación rompe CI.
- Semantics / accesibilidad (no solo color) alineado a `design-system`.

No recomposición por cada byte de balanza: el test del Screen usa un state ya filtrado.
