# EVID-REPORT-001-01 — API: reporte con filtros y aislamiento por tienda

> **Evidencia ilustrativa.** Resumen ficticio de una ejecución de CI.

| Campo | Valor |
|---|---|
| TEST | TEST-REPORT-001-03 |
| Entorno | CI backend (GitHub Actions, Testcontainers) |
| Build / commit | MarketjoyaBackend `b7e41c2` |
| Fecha | 2026-08-31 |
| Resultado | pasa |

```text
[TestId=TEST-REPORT-001-03] GetCashSessionReport_filters_by_register_and_company   Passed  640 ms
  GET /api/v1/reports/cash-sessions?date=2026-08-31                  → 200, 2 filas (tienda A)
  GET /api/v1/reports/cash-sessions?date=2026-08-31&cashRegisterId=… → 200, 1 fila
```
