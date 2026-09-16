# Checklist: UI nueva (web o Android)

- [ ] Leídos tokens semánticos y el `Mr*` que aplica (si el renderer aún no existe, se construye antes o se documenta el gap).
- [ ] Sin HEX/RGB, sin dp/sp/px si hay token, sin tipo suelto.
- [ ] Sin variante local; si faltaba, se extendió el DS primero.
- [ ] Web: `var(--mr-*)`; Tailwind solo layout, sin valores arbitrarios; estilos en CSS.
- [ ] Android: dentro de `MrTheme`; `MrTheme.colors` / `MrTheme.typography`; no Material crudo si hay `Mr*`.
- [ ] Estados (loading/disabled/error) no solo por color.
- [ ] Targets táctiles (≥48 Android, ≥44 web).
- [ ] POS: `design-system/rules/pos-rules.json` si es caja/pesaje/pago.
