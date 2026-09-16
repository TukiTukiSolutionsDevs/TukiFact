# EVID-CASH-001-07 — POS: apertura sobre el umbral queda pendiente (Maestro, 1.1.0)

> **Evidencia ilustrativa.** Re-ejecución ficticia de TEST-CASH-001-14, modificada por CHG-001.

| Campo | Valor |
|---|---|
| TEST | TEST-CASH-001-14 |
| Entorno | Emulador Android API 34 contra QA backend con `CashRegister:ApprovalThresholdEnabled=true` |
| Build / commit | app-pos `8b27c44`, backend `5e8a1f0` |
| Fecha | 2026-09-29 |
| Ejecutado por | qa (ejemplo) |
| Resultado | pasa |

```text
$ maestro test maestro/cash-register/open-cash-session.yaml
 ✅ Run flow maestro/auth/pos-login.yaml (cajero01)
 ✅ Input text "200.00" · Double tap "cash-opening-submit" · Assert "Caja abierta"
 ✅ setAirplaneMode enabled · "150.00" en Caja 02 · Assert "Pendiente de sincronizar" · reconectar · Assert "Caja abierta"
 ✅ setAirplaneMode enabled · "800.00" · Assert visible "Para abrir con más de S/ 500.00 necesitas conexión"
 ✅ setAirplaneMode disabled · "800.00" en Caja 03 · Assert visible "cash-opening-status" = "Pendiente de aprobación"
 ✅ (aprobación hecha por la herramienta de datos de QA) · Assert visible "Caja abierta" (timeout 15 s)
Flow passed in 71.8 s
```
