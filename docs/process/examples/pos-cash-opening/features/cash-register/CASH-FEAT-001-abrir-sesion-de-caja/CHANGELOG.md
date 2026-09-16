# Changelog — CASH-FEAT-001 Abrir sesión de caja

> **Ejemplo ilustrativo.** Historial de versiones de la especificación del feature en el ejemplo `pos-cash-opening`; responsables, archivos y fechas son ficticios.

## [1.1.0] — 2026-09-17

| Campo | Valor |
|---|---|
| Cambio | CHG-001 |
| Tipo | minor |
| Descripción | Aprobación de supervisor para aperturas mayores a S/ 500.00: apertura pendiente, aprobación, rechazo y bloqueo de autoaprobación; sin conexión solo hasta el umbral |
| Motivo | REQ-008 (Q-PRD-002 resuelta por la jefatura de caja, en el ejemplo) |
| Responsable | analista, arquitecto y qa (ejemplo) |
| Requisitos | REQ-008 (nuevo), REQ-002 y REQ-007 (aclarados) |
| Criterios afectados | AC-CASH-001-01 (modificado), AC-CASH-001-04 (modificado), AC-CASH-001-06 (nuevo), AC-CASH-001-07 (nuevo), AC-CASH-001-08 (nuevo), AC-CASH-001-09 (nuevo) |
| Archivos técnicos | backend: src/Modules/CashRegister/**/ApproveCashSessionOpening/*, …/RejectCashSessionOpening/*, …/GetCashSession/*, …/GetPendingCashSessionApprovals/*, CashRegisterOptions.cs, Migrations/*_AddCashSessionApproval.cs, openapi/marketjoya-api-v1.json; mobile: domain/…/cashregister/ObserveCashSessionDecisionUseCase.kt, MarketjoyaMigrations.kt (MIGRATION_3_4), feature/cash-register/*; front: src/core/cash-register/*, src/data/cash-register/*, src/infrastructure/http/cash-register/*, src/ui/cash-register/pending-approvals/*, e2e/cash-register/approve-cash-session-opening.spec.ts |
| Pruebas | TEST-CASH-001-17 a TEST-CASH-001-24 (nuevas); TEST-CASH-001-05 y TEST-CASH-001-14 (modificadas) |
| Features relacionados | REPORT-FEAT-001 (inducido, 1.0.1) |
| Riesgos | App POS 1.0.0 sin soporte de requiresApproval (mitigado con la configuración CashRegister:ApprovalThresholdEnabled) |
| Validación | aprobado |
| Liberado en | REL-2026.10.1 |

## [1.0.0] — 2026-08-11

| Campo | Valor |
|---|---|
| Cambio | — |
| Tipo | initial |
| Descripción | Creación del feature |
| Motivo | Alta inicial desde el PRD (REQ-001, REQ-002) |
| Responsable | analista (ejemplo) |
| Requisitos | REQ-001, REQ-002, REQ-005, REQ-006, REQ-007 |
| Criterios afectados | AC-CASH-001-01 a AC-CASH-001-05 (nuevos) |
| Archivos técnicos | backend: src/Modules/CashRegister/**, src/BuildingBlocks/Marketjoya.BuildingBlocks.Contracts/CashRegister/CashSessionOpenedIntegrationEvent.cs, src/Common/Marketjoya.Common.Infrastructure/Persistence/UniqueConstraintErrorMap.cs, openapi/marketjoya-api-v1.json; mobile: domain/…/cashregister/*, core/network/…/cashregister/*, core/database/…/cashregister/*, feature/cash-register/*, maestro/cash-register/open-cash-session.yaml |
| Pruebas | TEST-CASH-001-01 a TEST-CASH-001-15 (nuevas); TEST-CASH-001-16 (nueva, agregada por BUG-001) |
| Features relacionados | AUTH-FEAT-001 (dependencia), REPORT-FEAT-001 (consumidor del evento) |
| Riesgos | Sesiones duplicadas por reintentos o dos dispositivos |
| Validación | aprobado |
| Liberado en | REL-2026.09.1 |
