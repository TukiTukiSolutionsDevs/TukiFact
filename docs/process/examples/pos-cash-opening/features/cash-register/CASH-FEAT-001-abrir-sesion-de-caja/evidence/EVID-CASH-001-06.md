# EVID-CASH-001-06 — Web: consola de aprobación de aperturas (Playwright)

> **Evidencia ilustrativa.** Resumen ficticio de una ejecución de la versión 1.1.0 (CHG-001); el reporte HTML original quedó como artefacto de CI.

| Campo | Valor |
|---|---|
| TEST | TEST-CASH-001-22 |
| Entorno | QA web, Chromium 129 y Firefox 130; QA backend con el umbral activo |
| Build / commit | MarketjoyaFront `3d9e6b2`, backend `5e8a1f0` |
| Fecha | 2026-09-29 |
| Resultado | pasa |

```text
$ npm run e2e -- e2e/cash-register/approve-cash-session-opening.spec.ts
  ✓ [chromium] TEST-CASH-001-22 supervisor aprueba la apertura pendiente de Caja 01 (3.2s)
  ✓ [chromium] TEST-CASH-001-22 supervisor rechaza la apertura pendiente de Caja 01 (2.9s)
  ✓ [chromium] TEST-CASH-001-22 segundo supervisor ve "Esta apertura ya fue decidida" (4.1s)
  ✓ [firefox]  TEST-CASH-001-22 supervisor aprueba la apertura pendiente de Caja 01 (3.6s)
  ✓ [firefox]  TEST-CASH-001-22 supervisor rechaza la apertura pendiente de Caja 01 (3.1s)
  ✓ [firefox]  TEST-CASH-001-22 segundo supervisor ve "Esta apertura ya fue decidida" (4.4s)
  6 passed (21.9s)
```
