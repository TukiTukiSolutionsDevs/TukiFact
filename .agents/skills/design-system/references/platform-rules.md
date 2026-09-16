# Reglas de plataforma

Fuente: `design-system/rules/platform-rules.json`.

## Web (`MarketjoyaFront`)

- Pack: Angular Material 3 **como base del renderer**, no como identidad suelta en features. `@angular/material` no está instalado (ver pendientes).
- Tailwind v4 vía `@tailwindcss/postcss`. `src/styles.css` importa, en orden: `tailwindcss`, `styles/tailwind-layout-only.css`, `styles/generated/tailwind.theme.css`, `styles/generated/tokens.css`.
- `tailwind-layout-only.css` anula los namespaces de color, font, text, tracking, leading, radius, shadow, blur, perspective, ease, animate, spacing y breakpoint; el theme generado vuelve a añadir breakpoints (sm 640, md 768, lg 1024, xl 1280, xxl 1536 px) y spacing 0–8 del DS.
- Efecto: `bg-red-500`, `rounded-lg`, `shadow-md`, `font-sans`, `text-lg` no generan CSS; `flex`, `grid`, `p-4`, `gap-5`, `md:`/`xxl:` sí; `p-9` no existe. Lo prueba `tools/tailwind/tailwind-layout-only.spec.mts`.
- Tailwind **permitido**: layout, grid, flex, responsive, spacing estructural. **Prohibido**: identidad de componente, colores de marca, radius arbitrario.
- Valores arbitrarios (`bg-[#fff]`, `rounded-[6px]`) no se pueden anular por theme: se rechazan en revisión.
- Estilos globales y de componente en CSS, no SCSS (Tailwind v4).
- Valores visuales: `var(--mr-*)`.

## Android (`MarketjoyaMobile/core/designsystem`)

- Compose Material 3 como dependencia `implementation`: no se expone a features; foundation/ui como `api`.
- `MrTheme { }` es la raíz de ambas apps: `MaterialTheme` con `MrMaterialMapping` (color scheme claro + typography) y `LocalMinimumInteractiveComponentSize` = `MrInteractionTokens.minimumTouchTarget` (≥48dp, cubierto por `MrThemeRulesTest`).
- Acceso: `MrTheme.colors`, `MrTheme.typography` (`MrTypography`, con `moneyLarge`/`weightLarge` de números tabulares).
- Material 3 solo dentro del renderer `Mr*`. Si existe `MrX`, la feature no usa el componente Material equivalente.

## Pendiente de decisión

- Fuentes Inter / Roboto Mono: el pack las nombra pero no incluye archivos. Android usa `FontFamily.SansSerif` / `Monospace`; web emite `"Inter"` / `"Roboto Mono"` en `tokens.css` sin cargar fuentes y declara fallback `system-ui, sans-serif` en `src/styles.css`.
- Tokens sin equivalente para roles Material `onError`, `outline`, `tertiary`, containers e inverse: Android usa los defaults de Material.
- Paleta oscura: el pack define solo una paleta clara; no hay dark ni dynamic color.
- Incorporar Angular Material 3 en web al construir los `Mr*`.
