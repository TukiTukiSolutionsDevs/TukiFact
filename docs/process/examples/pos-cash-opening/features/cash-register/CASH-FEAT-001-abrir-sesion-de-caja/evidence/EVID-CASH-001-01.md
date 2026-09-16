# EVID-CASH-001-01 — API: apertura y caja ya abierta (E2E HTTP)

> **Evidencia ilustrativa.** Resumen ficticio de una ejecución de CI; el enlace y los hashes no existen.

| Campo | Valor |
|---|---|
| TEST | TEST-CASH-001-06 |
| Entorno | CI backend (GitHub Actions, Testcontainers Postgres 17 y RabbitMQ 4) |
| Build / commit | MarketjoyaBackend `b7e41c2` |
| Ejecución de CI | https://github.com/ejemplo/MarketjoyaBackend/actions/runs/1000000001 (artefacto descargado aquí porque caduca) |
| Fecha | 2026-08-31 |
| Resultado | pasa |

Extracto del TRX:

```text
[TestId=TEST-CASH-001-06] OpenCashSession_returns_200_and_then_409_AlreadyOpen   Passed  812 ms
  POST /api/v1/cash-sessions → 200 {"cashSessionId":"…","openingAmount":200.00}
  POST /api/v1/cash-sessions → 409 code=CashSession.AlreadyOpen traceId=00-… correlationId=e2e-cash-06
```
