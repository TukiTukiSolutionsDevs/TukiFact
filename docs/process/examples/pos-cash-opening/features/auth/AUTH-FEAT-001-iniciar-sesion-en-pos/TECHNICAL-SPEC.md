---
feature: AUTH-FEAT-001
spec_version: 1.0.0
status: reviewed
adrs: []
impact:
  modules_direct: [AUTH]
  modules_indirect: [CASH]
  features_affected: []
  regression_tests: []
  contracts: ["openapi:Identity_StartPosSession"]
  migrations: true
updated: 2026-08-11
---

# Especificación técnica — AUTH-FEAT-001 Iniciar sesión en POS

> **Ejemplo ilustrativo y mínimo.** Rutas y nombres siguen las skills de arquitectura, pero el endpoint y la tabla son del ejemplo: en Market Real el contrato de autenticación sigue pendiente de decisión.

## Resumen

Endpoint anónimo en el módulo backend `Identity` que valida credenciales y emite el JWT con el claim `cash_register` del dispositivo; pantalla de inicio de sesión en `app-pos`. Repos: backend y mobile.

## Arquitectura actual involucrada

- Backend: `src/Modules/Identity/` (4 proyectos), `Common.Infrastructure/Authentication` (`JwtOptions`, `CustomClaims`), `ICurrentUser.CashRegisterId`.
- Mobile: `core/network` (`HttpErrorMapper`), `domain/error` (`AppResult`, `AppError`).

## Reutilización

- Se reutiliza la emisión de JWT HS256 y los claims existentes (`sub`, `company`, `cash_register`, `permissions`).
- Se reutiliza `HttpErrorMapper` para traducir ProblemDetails a `AppError`.
- Descartado: un módulo de autenticación nuevo; `Identity` ya es el dueño de usuarios.

## Componentes nuevos

- Caso de uso `StartPosSession` y tabla `identity.pos_device_assignments`. Sin servicios, colas ni eventos nuevos.

## Backend

- `StartPosSessionCommand(string UserName, string Password, string DeviceId)` + validador (`UserName.Required`, `Password.Required`).
- `StartPosSessionEndpoint`: `POST /api/v1/auth/pos-sessions`, `.AllowAnonymous()`, `.WithName("StartPosSession")` → `Identity_StartPosSession`, `.WithStandardProblems()`.
- Error `IdentityErrors.InvalidCredentials` (`Auth.InvalidCredentials`, `Validation`).

## Frontend

No aplica — el feature es solo para el POS.

## Mobile

- `feature/auth`: `StartPosSessionUseCase` → puerto `PosSessionGateway` → adapter HTTP; token guardado en almacenamiento cifrado.
- Pantalla Compose `PosLoginScreen` con componentes `Mr*`.

## Base de datos y migraciones

- Migración `AddPosDeviceAssignments` en el schema `identity`: tabla `pos_device_assignments(device_id, cash_register_id)`.

## Contratos

- Nuevo `Identity_StartPosSession` (`openapi:Identity_StartPosSession`). Consumidor: mobile.

## Eventos y mensajería

No aplica — el inicio de sesión no publica eventos.

## Integraciones externas

No aplica — sin integraciones externas.

## Seguridad y autorización

- Endpoint anónimo con rate limit del borde (429 → `Network.RateLimited`). Contraseña nunca registrada en logs.
- El mensaje de error no revela si el usuario existe.

## Observabilidad

- Traza `marketjoya.usecase.Identity.StartPosSession` del `TelemetryBehavior`; `X-Correlation-Id` enviado por el POS.

## Errores

| code | type | HTTP |
|---|---|---|
| StartPosSession.Validation | VALIDATION | 400 |
| Auth.InvalidCredentials | VALIDATION | 400 |

## Concurrencia y transacciones

No aplica — operación de solo lectura de credenciales; la emisión del token no modifica agregados.

## Compatibilidad y datos existentes

- Backfill de `pos_device_assignments` para los POS existentes antes del rollout.

## Despliegue y reversión

- Migración antes del rollout del API; reversión: retirar la versión del API (la tabla nueva no rompe a nadie).

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| POS sin asignación de caja | media | alto | Backfill verificado antes del rollout |

## Análisis de impacto

- Módulos directos: AUTH. Indirectos: CASH (lee `cash_register` del token).
- Endpoints, eventos, reportes, notificaciones, integraciones: No aplica — solo el endpoint nuevo.
- Regresión: No aplica — no hay features previos.

## Archivos a crear o modificar

- backend: `src/Modules/Identity/Marketjoya.Modules.Identity.Application/PosSessions/StartPosSession/StartPosSessionCommand.cs`, `…/StartPosSessionCommandHandler.cs`, `…/StartPosSessionCommandValidator.cs`; `src/Modules/Identity/Marketjoya.Modules.Identity.Presentation/PosSessions/StartPosSession/StartPosSessionEndpoint.cs`; `openapi/marketjoya-api-v1.json`
- mobile: `feature/auth/src/main/kotlin/pe/marketjoya/feature/auth/StartPosSessionUseCase.kt`, `feature/auth/src/main/kotlin/pe/marketjoya/feature/auth/ui/PosLoginScreen.kt`

## Orden de implementación

1. backend: migración y caso de uso (TEST-AUTH-001-01, TEST-AUTH-001-02).
2. backend: endpoint y OpenAPI.
3. mobile: caso de uso, pantalla y flujo Maestro (TEST-AUTH-001-03).
