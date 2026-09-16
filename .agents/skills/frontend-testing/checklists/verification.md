# Checklist: verificación frontend

Ejecutar en `MarketjoyaFront/`.

- [ ] Specs del slice junto a los archivos tocados (`core` + adapter o service).
- [ ] `npm test` en verde (unit + tooling).
- [ ] `npm run lint:architecture` si cambió `eslint.config.js` o un fixture.
- [ ] `npm run e2e` si el flujo es crítico o ya hay spec E2E de esa pantalla.
- [ ] `npm run verify` en verde antes de terminar.
- [ ] Comando anotado con resultado, o motivo de skip.

```bash
npm run test:watch   # iteración
npm test             # unit + tooling
npm run e2e          # Playwright
npm run verify       # format:check, lint, typecheck, tokens:check, test, build
```
