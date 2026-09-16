# EVID-CASH-001-03 — POS: apertura con doble toque, sin conexión y reconexión (Maestro)

> **Evidencia ilustrativa.** Re-ejecución ficticia de TEST-CASH-001-14 tras la corrección de BUG-001.

| Campo | Valor |
|---|---|
| TEST | TEST-CASH-001-14 |
| Entorno | Emulador Android API 34 contra QA backend; latencia 800 ms |
| Build / commit | app-pos `d41f7e2` (incluye el fix de BUG-001), backend `b7e41c2` |
| Fecha | 2026-08-31 |
| Ejecutado por | qa (ejemplo) |
| Resultado | pasa (3 ejecuciones consecutivas) |

```text
$ maestro test maestro/cash-register/open-cash-session.yaml
 ✅ Launch app "pe.marketjoya.pos" (clearState)
 ✅ Run flow maestro/auth/pos-login.yaml (cajero01)
 ✅ Input text "200.00" on "cash-opening-amount"
 ✅ Double tap on "cash-opening-submit"
 ✅ Assert visible "cash-opening-status" = "Caja abierta"
 ✅ Assert not visible "La caja ya tiene una sesión abierta"
 ✅ setAirplaneMode enabled · abrir "Caja 02" con 150.00
 ✅ Assert visible "cash-opening-status" = "Pendiente de sincronizar"
 ✅ setAirplaneMode disabled · Assert visible "cash-opening-status" = "Caja abierta" (timeout 20 s)
Flow passed in 48.2 s
```

Verificación en QA backend: una sola fila `Open` por caja para `pos-qa-01` en `cash_register.cash_sessions`.
