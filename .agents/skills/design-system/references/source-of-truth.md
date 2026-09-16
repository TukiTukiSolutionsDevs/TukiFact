# Fuente de verdad

El pack declarativo está en la raíz del repo, no en cada app:

```text
design-system/
├── tokens/                 colores, spacing, typography, radius, motion, … (*.json)
├── rules/                  platform-rules.json, pos-rules.json
├── components/             button, card, dialog, status-chip, text-field
├── business-components/    money-display, weight-display, sale-total, receipt-preview, payment-method-card, device-status
└── schemas/component.schema.json
```

Meta: `design-system/tokens/meta.json` — Market Real Design System `1.0.0`, strategy `framework-agnostic`, plataformas `angular` y `android-compose`.

Angular y Compose son **renderizadores**. Cambiar un HEX en una Screen es un bug; se cambia el token y se regenera.

## Generadores por plataforma

| | Web (`MarketjoyaFront`) | Android (`MarketjoyaMobile`) |
|---|---|---|
| Generador | `tools/tokens/` (Node) | `build-logic` → `DesignTokensConventionPlugin` |
| Lee | `tokens/*.json` | `tokens/*.json` + `rules/pos-rules.json` |
| Salida versionada | `src/styles/generated/tokens.css`, `src/styles/generated/tailwind.theme.css` | `core/designsystem/src/main/kotlin/pe/marketjoya/core/designsystem/generated/Mr*Tokens.kt` |
| Regenerar | `npm run tokens` | `./gradlew :core:designsystem:generateDesignTokens` |
| Verificar | `npm run tokens:check` (falla si la salida está desactualizada; incluido en `npm run verify`) | `./gradlew :core:designsystem:checkDesignTokens` (enganchado a `check`) |
| Otra ubicación del pack | `DESIGN_SYSTEM_DIR` o `--source <dir>` (default `../design-system`) | `-Pmarketjoya.designSystemDir=<dir>` (default `../design-system`) |

Los archivos generados llevan cabecera "GENERATED FILE - DO NOT EDIT" (web) y no se editan a mano en ninguna plataforma.

Detalle de nombres: [tokens.md](tokens.md). Consumo por plataforma: [platform-rules.md](platform-rules.md).
