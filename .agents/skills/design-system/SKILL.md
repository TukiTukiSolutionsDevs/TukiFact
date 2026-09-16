---
name: design-system
description: "Trigger: design system, token, MrButton, color, spacing, estilo visual en Compose, Tailwind de marca. Contrato visual central de Market Real."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "1.2"
---

# Design System — Market Real

## Activation Contract

Contrato visual único para Angular (`MarketjoyaFront`) y Compose (`MarketjoyaMobile`). Los valores viven en `design-system/`; cada plataforma genera código versionado desde el JSON. Rutas `design-system/`, `MarketjoyaFront/`, `MarketjoyaMobile/` relativas a la raíz del repo. Activa al pintar UI, crear componente, token, variante o pantalla POS/web, o ante un color, `dp`, HEX o clase Tailwind de marca.

## Hard Rules

- Fuente de verdad: `design-system/tokens/`, `rules/`, `components/`, `business-components/`.
- Salidas generadas (web `src/styles/generated/`, Android `core/designsystem/.../generated/`) no se editan a mano.
- Features usan semánticos: `var(--mr-*)` en web, `MrTheme` en Android. Nunca primitivos, HEX/RGB, dp/sp/px con token existente ni tipografía suelta.
- Sin variantes locales ni clones de `Mr*`: se extiende el DS primero. Si existe `MrX`, no uses el Material/`<button>` crudo equivalente.
- Tailwind v4 solo layout. Valores arbitrarios (`bg-[#fff]`) no los bloquea el theme: se rechazan en revisión.
- Error/estado nunca solo por color. Touch ≥48dp Android, ≥44px web. POS: `design-system/rules/pos-rules.json`.

## Decision Gates

| Situación | Acción |
|---|---|
| Color, espacio, tipo, radio, elevación | Token semántico de `design-system/tokens/` |
| Cambiaste tokens o `pos-rules.json` | Regenerar y verificar en ambas plataformas (`checklists/new-token.md`) |
| Botón, campo, chip, card, dialog | `design-system/components/`; ningún `Mr*` implementado aún (`references/components.md`) |
| Dinero, peso, ticket, pago, dispositivo | `design-system/business-components/` |
| Falta una variante | Extender el JSON, luego el renderer |
| Layout web | Tailwind estructural (breakpoints y spacing 0–8 del DS) |
| Dark mode, fuentes, roles Material sin token | Pendiente de decisión (`references/platform-rules.md`) |

## Execution Steps

1. Lee `references/source-of-truth.md` y el JSON aplicable.
2. Pantalla: `checklists/new-ui.md`. Contrato nuevo: `checklists/new-component.md` o `checklists/new-token.md`.
3. Tras cambiar JSON: `npm run tokens` (`MarketjoyaFront/`) y `./gradlew :core:designsystem:generateDesignTokens` (`MarketjoyaMobile/`).
4. Renderer **después** del JSON; aplica `references/platform-rules.md` y POS si aplica.
5. Verifica: `npm run tokens:check` y `./gradlew :core:designsystem:checkDesignTokens`.
6. Revisión: `checklists/review.md`.

## Output Contract

- JSON de token/componente usado.
- Renderer (Angular / Compose) o "solo contrato"; variantes y estados cubiertos.
- Comandos de regeneración y check por plataforma, o motivo de skip.
- Bloqueadores: HEX, dp suelto, Material crudo, Tailwind de marca o arbitrario, variante local, generado editado a mano.

## References

- [references/source-of-truth.md](references/source-of-truth.md), [references/tokens.md](references/tokens.md)
- [references/components.md](references/components.md), [references/platform-rules.md](references/platform-rules.md), [references/pos-rules.md](references/pos-rules.md)
- [templates/](templates/), [checklists/](checklists/)
- [../hexaclean-architecture/SKILL.md](../hexaclean-architecture/SKILL.md), [../mobile-architecture/SKILL.md](../mobile-architecture/SKILL.md)
