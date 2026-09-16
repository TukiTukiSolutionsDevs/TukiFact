# EVID-CASH-001-05 — API: permisos y autoaprobación de aperturas (E2E HTTP)

> **Evidencia ilustrativa.** Resumen ficticio de una ejecución de CI de la versión 1.1.0 (CHG-001); el enlace y los hashes no existen.

| Campo | Valor |
|---|---|
| TEST | TEST-CASH-001-21 |
| Entorno | CI backend (GitHub Actions, Testcontainers Postgres 17 y RabbitMQ 4) |
| Build / commit | MarketjoyaBackend `5e8a1f0` |
| Ejecución de CI | https://github.com/ejemplo/MarketjoyaBackend/actions/runs/1000000042 (artefacto descargado aquí) |
| Fecha | 2026-09-29 |
| Resultado | pasa |

Extracto del TRX:

```text
[TestId=TEST-CASH-001-21] Approve_as_cashier_returns_403                          Passed  104 ms
[TestId=TEST-CASH-001-21] Approve_from_other_company_returns_404_NotFound         Passed  133 ms
[TestId=TEST-CASH-001-21] Approve_own_opening_returns_409_SelfApprovalNotAllowed  Passed  121 ms
[TestId=TEST-CASH-001-21] Second_decision_returns_409_NotPendingApproval          Passed  158 ms
  todas las respuestas de error con code, traceId y correlationId (403 sin code)
```
