# EVID-REPORT-001-03 — Web: reporte de sesiones de caja (Playwright)

> **Evidencia ilustrativa.** Resumen ficticio de una ejecución; el reporte HTML original quedó como artefacto de CI.

| Campo | Valor |
|---|---|
| TEST | TEST-REPORT-001-06 |
| Entorno | QA web, Chromium 128 y Firefox 129 |
| Build / commit | MarketjoyaFront `e52a9c7` |
| Fecha | 2026-08-31 |
| Resultado | pasa |

```text
$ npm run e2e -- e2e/reports/cash-session-report.spec.ts
  ✓ [chromium] TEST-REPORT-001-06 muestra la sesión abierta de Caja 01 (2.1s)
  ✓ [chromium] TEST-REPORT-001-06 muestra "No tienes permiso para ver este reporte" al cajero (1.3s)
  ✓ [firefox]  TEST-REPORT-001-06 muestra la sesión abierta de Caja 01 (2.6s)
  ✓ [firefox]  TEST-REPORT-001-06 muestra "No tienes permiso para ver este reporte" al cajero (1.5s)
  4 passed (9.8s)
```
