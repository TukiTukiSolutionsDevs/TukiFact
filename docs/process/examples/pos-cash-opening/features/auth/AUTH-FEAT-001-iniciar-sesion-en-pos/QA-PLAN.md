---
feature: AUTH-FEAT-001
spec_version: 1.0.0
status: passed
last_run: 2026-08-20
updated: 2026-08-20
---

# Plan de QA — AUTH-FEAT-001 Iniciar sesión en POS

> **Ejemplo ilustrativo y mínimo.** Plan del feature dependencia del ejemplo `pos-cash-opening`; rutas de prueba y resultados son ficticios.

## Objetivo

Demostrar que el cajero inicia sesión con credenciales válidas, que la sesión trae la caja del dispositivo y que las credenciales inválidas no abren sesión.

## Alcance

US-AUTH-001-01 (AC-AUTH-001-01, AC-AUTH-001-02) en backend y mobile.

## Fuera de alcance

Renovación y cierre de sesión (fuera del feature).

## Criterios de entrada

- [x] Feature en ready-for-qa (G-QA)
- [x] CI verde en backend y mobile
- [x] POS de QA asignado a "Caja 01"

## Criterios de salida

- [x] Todo TEST en pasa
- [x] EVID por cada TEST e2e
- [x] Ningún BUG critical/major abierto

## Entornos

| Entorno | URL o dispositivo | Versión / commit | Notas |
|---|---|---|---|
| QA backend | https://api.qa.ejemplo.invalid | backend `4c1d2e9` | Postgres y RabbitMQ de QA |
| POS de QA | Emulador Android API 34 | app-pos `7a3f0b1` | Maestro 1.x |

## Datos de prueba

Usuario `cajero01` (contraseña de QA en el gestor de secretos, nunca en el plan) y POS `pos-qa-01` asignado a "Caja 01".

## Matriz de roles y permisos

| Rol | Acción | Esperado | TEST |
|---|---|---|---|
| Cajero activo | Iniciar sesión | permitido, sesión con caja | TEST-AUTH-001-01 |
| Usuario con contraseña incorrecta | Iniciar sesión | denegado con mensaje genérico | TEST-AUTH-001-02 |

## Casos de prueba

| ID | Criterios | Tipo | Nivel | Repo | Automatización | Resultado |
|---|---|---|---|---|---|---|
| TEST-AUTH-001-01 | AC-AUTH-001-01 | positivo | integration | backend | tests/Marketjoya.Modules.Identity.IntegrationTests/PosSessions/StartPosSessionTests.cs | pasa |
| TEST-AUTH-001-02 | AC-AUTH-001-02 | negativo | integration | backend | tests/Marketjoya.Modules.Identity.IntegrationTests/PosSessions/StartPosSessionTests.cs | pasa |
| TEST-AUTH-001-03 | AC-AUTH-001-01, AC-AUTH-001-02 | positivo | e2e | mobile | maestro/auth/pos-login.yaml | pasa |

## Regresión

| TEST | Feature | Motivo | Resultado |
|---|---|---|---|

## No funcionales

- Seguridad: el mensaje de credenciales inválidas no revela si el usuario existe (verificado en TEST-AUTH-001-02).
- Rendimiento, accesibilidad, migración: No aplica — fuera del alcance mínimo del ejemplo.

## Evidencias

| ID | TEST | Archivo o enlace | Fecha |
|---|---|---|---|
| EVID-AUTH-001-01 | TEST-AUTH-001-03 | EVID-AUTH-001-01.md | 2026-08-20 |

## Resultados

| Fecha | Entorno | Build / commit | Ejecutados | Pasan | Fallan | Bloqueados | Responsable |
|---|---|---|---|---|---|---|---|
| 2026-08-20 | QA backend + emulador POS | backend `4c1d2e9`, app-pos `7a3f0b1` | 3 | 3 | 0 | 0 | qa (ejemplo) |
