# EVID-AUTH-001-01 — Inicio de sesión en POS (Maestro)

> **Evidencia ilustrativa.** Resumen ficticio de una ejecución; muestra el contenido mínimo exigido por `docs/process/06-automatizacion-de-pruebas.md`.

| Campo | Valor |
|---|---|
| TEST | TEST-AUTH-001-03 |
| Entorno | Emulador Android API 34 contra QA backend |
| Build / commit | app-pos `7a3f0b1`, backend `4c1d2e9` |
| Fecha | 2026-08-20 |
| Ejecutado por | qa (ejemplo) |
| Resultado | pasa |

Salida resumida (sin credenciales):

```text
$ maestro test maestro/auth/pos-login.yaml
 ✅ Launch app "pe.marketjoya.pos" (clearState)
 ✅ Input text on "login-user"
 ✅ Input text on "login-password" (valor enmascarado)
 ✅ Tap on "login-submit"
 ✅ Assert visible "cash-opening-register" = "Caja 01"
 ✅ Relaunch, contraseña incorrecta → Assert visible "Usuario o contraseña incorrectos"
Flow passed in 21.4 s
```
