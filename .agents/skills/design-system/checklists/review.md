# Checklist: revisión visual

- [ ] Todo valor visual sale de `design-system/` (`var(--mr-*)` en web, `MrTheme` / `Mr*Tokens` en Android).
- [ ] No hay HEX ni utilities de marca (`text-red-500`).
- [ ] No hay valores arbitrarios de Tailwind (`bg-[#…]`, `rounded-[…]`): el theme no los bloquea.
- [ ] No hay `MyButton` paralelo a `MrButton`.
- [ ] Error/estado con texto o icono, no solo color.
- [ ] POS cumple `design-system/rules/pos-rules.json` si aplica.
- [ ] Archivos generados no editados a mano; `npm run tokens:check` y `checkDesignTokens` en verde si cambió el pack.
- [ ] El cambio de token no se hizo "solo en Android" o "solo en web" sin nota.
