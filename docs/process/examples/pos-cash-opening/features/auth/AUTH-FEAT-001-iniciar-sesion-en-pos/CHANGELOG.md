# Changelog — AUTH-FEAT-001 Iniciar sesión en POS

> **Ejemplo ilustrativo.** Historial de versiones del feature dependencia del ejemplo `pos-cash-opening`.

## [1.0.0] — 2026-08-11

| Campo | Valor |
|---|---|
| Cambio | — |
| Tipo | initial |
| Descripción | Creación del feature |
| Motivo | Alta inicial desde el PRD (REQ-003) |
| Responsable | analista (ejemplo) |
| Requisitos | REQ-003 |
| Criterios afectados | AC-AUTH-001-01 (nuevo), AC-AUTH-001-02 (nuevo) |
| Archivos técnicos | backend: src/Modules/Identity/…/PosSessions/StartPosSession/*, openapi/marketjoya-api-v1.json; mobile: feature/auth/*, maestro/auth/pos-login.yaml |
| Pruebas | TEST-AUTH-001-01, TEST-AUTH-001-02, TEST-AUTH-001-03 (nuevas) |
| Features relacionados | CASH-FEAT-001 (consumidor de la sesión) |
| Riesgos | POS sin caja asignada |
| Validación | aprobado |
| Liberado en | REL-2026.09.1 |
