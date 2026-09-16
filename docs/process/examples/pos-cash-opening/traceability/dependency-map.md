<!-- GENERADO por `tools/traceability/trace build`. No editar a mano. -->

# Mapa de dependencias

Leyenda: `A --> B` = A depende de B (`depends_on`); `A -.-> B` = relacionado (`related`).

```mermaid
flowchart LR
  AUTH_FEAT_001["AUTH-FEAT-001<br/>Iniciar sesión en POS"]
  CASH_FEAT_001["CASH-FEAT-001<br/>Abrir sesión de caja"]
  REPORT_FEAT_001["REPORT-FEAT-001<br/>Reporte de sesiones de caja"]
  AUTH_FEAT_001 -.-> CASH_FEAT_001
  CASH_FEAT_001 --> AUTH_FEAT_001
  CASH_FEAT_001 -.-> REPORT_FEAT_001
  REPORT_FEAT_001 -.-> CASH_FEAT_001
```

## Quién depende de mí

| Feature | Dependientes (depends_on) | Relacionados (related) |
|---|---|---|
| AUTH-FEAT-001 | CASH-FEAT-001 | — |
| CASH-FEAT-001 | — | AUTH-FEAT-001, REPORT-FEAT-001 |
| REPORT-FEAT-001 | — | CASH-FEAT-001 |
