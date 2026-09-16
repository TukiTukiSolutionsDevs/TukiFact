# Tokens

Lee el JSON; no reescribas la paleta en código. Rutas relativas a la raíz del repo.

| Archivo | Uso |
|---|---|
| `design-system/tokens/colors.primitive.json` | Paleta cruda (solo el pack; no se genera para features) |
| `design-system/tokens/colors.semantic.json` | Lo que consume UI: `color.semantic.background.*`, `text.*`, `brand.*`, `status.*`, `device.*` |
| `design-system/tokens/spacing.json` | Espaciado |
| `design-system/tokens/typography.json` | Familias y escala tipográfica (incl. `money`, `weight`) |
| `design-system/tokens/radius.json` | Radios |
| `design-system/tokens/elevation.json` | Elevación |
| `design-system/tokens/motion.json` | Duración / easing |
| `design-system/tokens/breakpoints.json` | Cortes web |
| `design-system/tokens/interaction.json` | Focus, press, touch target |
| `design-system/tokens/accessibility.json` | Contraste, targets |
| `design-system/tokens/meta.json` | Nombre, versión y plataformas |

En el JSON se referencian semánticos (`{color.semantic.text.primary}`), no primitivos. Token nuevo: [templates/token.md](../templates/token.md) + [checklists/new-token.md](../checklists/new-token.md).

## Web (`MarketjoyaFront/src/styles/generated/`)

- `tokens.css`: `:root` con `--mr-` + ruta JSON en kebab-case. `color.semantic.*` pierde el segmento `semantic`: `color.semantic.text.primary` → `--mr-color-text-primary`.
- Grupos emitidos: breakpoints, color, elevation, interaction, motion (duraciones), radius, spacing, typography.
- No se emiten: `color.primitive`, `meta`, `layout`, `accessibility`, `motion.rules`. Segmento `web` se elimina del nombre; hojas `android` se omiten.
- Una categoría de token sin regla CSS hace fallar la generación: añade la regla en `tools/tokens/tokens.mts` a conciencia.
- `tailwind.theme.css`: solo `--breakpoint-*` (sm, md, lg, xl, xxl) y `--spacing-*` (0–8).
- Uso: `var(--mr-color-background-app)`.

## Android (`MarketjoyaMobile/core/designsystem/.../generated/`)

- Objetos: `MrColorTokens` (semánticos), `MrSpacingTokens`, `MrRadiusTokens`, `MrElevationTokens`, `MrTypographyTokens`, `MrMotionTokens`, `MrInteractionTokens`, `MrPosTokens` (desde `rules/pos-rules.json`).
- Uso en features: `MrTheme.colors.<token>` (p. ej. `textPrimary`) y `MrTheme.typography.<estilo>`.
