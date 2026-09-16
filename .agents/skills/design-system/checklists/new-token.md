# Checklist: token nuevo

- [ ] No existe ya uno semántico equivalente.
- [ ] Se añade en el JSON correcto de `design-system/tokens/` (`colors.semantic.json`, `spacing.json`, …), no en una feature.
- [ ] Las features referencian el semántico, no el primitivo.
- [ ] Contraste/accesibilidad revisados (`design-system/tokens/accessibility.json`).
- [ ] Web, en `MarketjoyaFront/`: `npm run tokens` y luego `npm run tokens:check`. Si la categoría es nueva, regla CSS añadida en `tools/tokens/tokens.mts` y `npm run test:tooling`.
- [ ] Android, en `MarketjoyaMobile/`: `./gradlew :core:designsystem:generateDesignTokens` y luego `./gradlew :core:designsystem:checkDesignTokens`.
- [ ] Archivos generados de ambas plataformas versionados en el mismo cambio, o el gap documentado.
- [ ] Ningún archivo generado editado a mano.
