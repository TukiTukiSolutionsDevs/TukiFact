# EVID-CASH-001-02 — API: permisos de apertura (E2E HTTP)

> **Evidencia ilustrativa.** Resumen ficticio de una ejecución de CI; el enlace y los hashes no existen.

| Campo | Valor |
|---|---|
| TEST | TEST-CASH-001-07 |
| Entorno | CI backend (GitHub Actions, Testcontainers) |
| Build / commit | MarketjoyaBackend `b7e41c2` |
| Ejecución de CI | https://github.com/ejemplo/MarketjoyaBackend/actions/runs/1000000001 |
| Fecha | 2026-08-31 |
| Resultado | pasa |

Extracto del TRX:

```text
[TestId=TEST-CASH-001-07] OpenCashSession_without_token_returns_401             Passed  95 ms
[TestId=TEST-CASH-001-07] OpenCashSession_without_permission_returns_403        Passed  101 ms
  403 sin code; traceId y correlationId presentes
```
