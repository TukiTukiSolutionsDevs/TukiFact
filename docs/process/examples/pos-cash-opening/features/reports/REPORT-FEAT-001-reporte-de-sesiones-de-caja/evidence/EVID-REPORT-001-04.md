# EVID-REPORT-001-04 — API: sesión aprobada aparece con la hora de aprobación (1.0.1)

> **Evidencia ilustrativa.** Re-ejecución ficticia de TEST-REPORT-001-03 para el cambio inducido por CHG-001.

| Campo | Valor |
|---|---|
| TEST | TEST-REPORT-001-03 |
| Entorno | CI backend (GitHub Actions, Testcontainers), eventos reales publicados por CashRegister |
| Build / commit | MarketjoyaBackend `5e8a1f0` |
| Fecha | 2026-09-29 |
| Resultado | pasa |

```text
[TestId=TEST-REPORT-001-03] GetCashSessionReport_filters_by_register_and_company    Passed  655 ms
[TestId=TEST-REPORT-001-03] GetCashSessionReport_shows_approved_session_at_approval_time  Passed  1.2 s
  apertura solicitada 07:58, aprobada 08:05 (Lima) → fila "Caja 03" con hora 08:05
  apertura rechazada → sin fila
```
