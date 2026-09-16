# Releases — Market Real (ejemplo)

> **Ejemplo ilustrativo.** Registro de releases del ejemplo `pos-cash-opening`; las fechas, tags y repos liberados son ficticios. Procedimiento real: `docs/process/07-git-y-ci.md` (sección "Release y released_in").

## Releases

| ID | Fecha | Repos y tags | Features | Notas |
|---|---|---|---|---|
| REL-2026.10.1 | 2026-10-02 | backend: REL-2026.10.1; mobile: REL-2026.10.1; front: REL-2026.10.1; padre: REL-2026.10.1 | CASH-FEAT-001 (1.1.0), REPORT-FEAT-001 (1.0.1), CHG-001 | Migración `AddCashSessionApproval` antes del rollout; `CashRegister:ApprovalThresholdEnabled` se activa cuando el 100 % de los POS tenga la app 1.1.0 |
| REL-2026.09.1 | 2026-09-07 | backend: REL-2026.09.1; mobile: REL-2026.09.1; front: REL-2026.09.1; padre: REL-2026.09.1 | AUTH-FEAT-001, CASH-FEAT-001, REPORT-FEAT-001 | Migraciones de `identity`, `cash_register` y `reports` aplicadas antes del rollout; incluye el fix de BUG-001 |
