---
feature: CASH-FEAT-001
spec_version: 1.1.0
status: reviewed
adrs: [ADR-001]
impact:
  modules_direct: [CASH]
  modules_indirect: [AUTH, REPORT]
  features_affected: [REPORT-FEAT-001]
  regression_tests: [TEST-AUTH-001-01, TEST-REPORT-001-01, TEST-REPORT-001-03]
  contracts:
    - "openapi:CashRegister_OpenCashSession"
    - "openapi:CashRegister_GetCashSession"
    - "openapi:CashRegister_GetPendingCashSessionApprovals"
    - "openapi:CashRegister_ApproveCashSessionOpening"
    - "openapi:CashRegister_RejectCashSessionOpening"
    - "event:CashSessionOpenedIntegrationEvent"
  migrations: true
updated: 2026-09-17
---

# Especificación técnica — CASH-FEAT-001 Abrir sesión de caja

> **Ejemplo ilustrativo.** Los nombres siguen las convenciones reales de `MarketjoyaBackend`, `MarketjoyaMobile` y `MarketjoyaFront` (skills de arquitectura), pero el módulo `CashRegister`, sus tablas, endpoints y pruebas **no existen**: son el diseño del ejemplo. Donde el ejemplo necesita algo que en Market Real sigue "Pendiente de decisión" (`AGENTS.md`), se indica explícitamente. Versión 1.1.0: las adiciones de CHG-001 están marcadas.

## Resumen

Nuevo módulo backend `CashRegister` (schema `cash_register`) con el agregado `CashSession` y el command `OpenCashSessionCommand`, expuesto en `POST /api/v1/cash-sessions` (`CashRegister_OpenCashSession`). Una sola sesión activa por caja la garantiza un índice único parcial y la idempotencia usa `client_mutation_id` (ADR-001). El hecho se publica como `CashSessionOpenedIntegrationEvent` para el módulo Reports. En mobile (`app-pos`), un caso de uso con camino sin conexión sobre el outbox `PendingSyncWriter`.

**1.1.0 (CHG-001):** aperturas mayores a S/ 500.00 quedan `PendingApproval`; el supervisor las aprueba o rechaza desde una pantalla nueva de la consola web (`MarketjoyaFront`). Cuatro operaciones nuevas en el OpenAPI; el evento de integración no cambia de forma. Repos: backend, mobile y front.

## Arquitectura actual involucrada

- backend (skill `backend-architecture`):
  - `src/Common/Marketjoya.Common.Application/Messaging/` — `ISender`, `ICommand` (fuerza `TransactionBehavior`).
  - `src/Common/Marketjoya.Common.Application/Behaviors/` — `ValidationBehavior` (`<UseCase>.Validation` con `errors`), `TransactionBehavior` sobre `ITransactionManager`, `TelemetryBehavior`.
  - `src/Common/Marketjoya.Common.Application/Abstractions/` — `IIntegrationEventPublisher`, `ICurrentUser.CashRegisterId`, `IClock`.
  - `src/Common/Marketjoya.Common.Infrastructure/Persistence/` — `BaseDbContext`, interceptores DomainEvents → Auditable → SyncVersion, `DbUpdateConcurrencyExceptionHandler`.
  - `src/Common/Marketjoya.Common.Presentation/Endpoints/` — `IEndpoint`, `WithStandardProblems`, `ToHttpResult`.
  - `src/BuildingBlocks/Marketjoya.BuildingBlocks.Contracts/` — `IntegrationEvent`.
  - Módulo de referencia para la estructura: `src/Modules/Identity/`. Desde 1.1.0, el propio `src/Modules/CashRegister/` de la versión 1.0.0.
- mobile (skill `mobile-architecture`):
  - `domain/src/main/kotlin/pe/marketjoya/domain/sync/` — `PendingSyncWriter`, `PendingSyncOperation`, `SyncIdentifiers` (`LocalId`, `ClientMutationId`), `SyncAttemptPolicy`.
  - `domain/src/main/kotlin/pe/marketjoya/domain/error/` — `AppResult`, `AppError`.
  - `core/network/src/main/kotlin/pe/marketjoya/core/network/error/HttpErrorMapper.kt`.
  - `core/database` — `MarketjoyaDatabase` (versión 2), `RoomPendingSyncWriter`; `core/testing/.../sync/FakePendingSyncWriter.kt`.
- front, 1.1.0 (skill `hexaclean-architecture`): `src/base/go-result.type.ts`, `src/infrastructure/http/to-app-error.mapper.ts`, `src/ui/app.routes.ts`, `src/ui/shell/app-shell.layout.ts`.
- Contrato de errores: `.agents/skills/backend-architecture/references/api-error-contract.md`.

## Reutilización

| Necesidad | Búsqueda | Resultado |
|---|---|---|
| Transacción del command | `rg -n "interface ITransactionManager" MarketjoyaBackend/src` | Se reutiliza `TransactionBehavior`; el handler nunca llama `SaveChanges` |
| Avisar a otro módulo | `rg -n "IIntegrationEventPublisher" MarketjoyaBackend/src` | Se reutiliza el outbox de Wolverine desde el handler del evento de dominio |
| Concurrencia optimista | `rg -n "SyncVersion" MarketjoyaBackend/src/Common` | Se reutiliza `sync_version`; en 1.1.0 resuelve la carrera entre supervisores |
| ProblemDetails y `code` | `rg -n "WithStandardProblems\|ToHttpResult" MarketjoyaBackend/src/Common` | Se reutilizan; el endpoint no construye errores a mano |
| Outbox mobile | `rg -n "interface PendingSyncWriter" MarketjoyaMobile/domain` | Se reutiliza `enqueue` idempotente por `ClientMutationId` |
| Errores HTTP en mobile | `rg -n "object HttpErrorMapper\|class HttpErrorMapper" MarketjoyaMobile/core` | Se reutiliza `HttpErrorMapper` |
| Violación de índice único → `code` | `rg -n "23505\|UniqueViolation" MarketjoyaBackend/src` | **No existía**: se agregó `UniqueConstraintErrorMap` en 1.0.0 (ADR-001); 1.1.0 lo reutiliza |
| Consumer de negocio entre módulos | `rg -n "IIntegrationEventConsumer<" MarketjoyaBackend/src` | El de Reports (1.0.0); 1.1.0 no agrega consumers |
| Errores HTTP en front (1.1.0) | `rg -n "export function toAppError" MarketjoyaFront/src` | Se reutiliza `toAppError` y `GoResult` |
| Componentes visuales (1.1.0) | `rg -o "Mr[A-Z][A-Za-z]+" design-system` | Se reutilizan `MrCard`, `MrMoney`, `MrButton`, `MrDialog`, `MrStatusChip` |
| Avisar al POS de la decisión (1.1.0) | `rg -n "push\|FirebaseMessaging" MarketjoyaMobile` | No hay infraestructura de notificaciones; se usa consulta periódica sobre el endpoint nuevo |

Descartado: lock distribuido con `AddCommonRedis` para serializar aperturas; notificaciones push para la decisión del supervisor (infraestructura nueva sin requisito).

## Componentes nuevos

- 1.0.0 — Módulo backend `CashRegister` (4 proyectos, schema `cash_register`) según `templates/module.md` de `backend-architecture`.
- 1.0.0 — Evento de integración `CashSessionOpenedIntegrationEvent` (contrato nuevo entre módulos) — ADR-001.
- 1.0.0 — `UniqueConstraintErrorMap` en `Common.Infrastructure/Persistence` — ADR-001.
- 1.0.0 — Mobile: paquete `domain/cashregister`, módulo `:feature:cash-register`, tabla Room `cash_sessions` (versión 3).
- 1.1.0 — Feature hexaclean `cash-register` en `MarketjoyaFront` (pantalla de aprobaciones). Sin servicios, colas, eventos de integración ni patrones nuevos: no requiere ADR nuevo.

## Backend

```text
src/Modules/CashRegister/
├── Marketjoya.Modules.CashRegister.Domain/CashSessions/
│   CashSession.cs · CashSessionId.cs · CashSessionStatus.cs · OpeningAmount.cs · CashSessionErrors.cs
│   CashSessionOpenedDomainEvent.cs · CashSessionApprovalRequestedDomainEvent.cs (1.1.0)
│   SupervisorMustDifferFromCashierRule.cs (1.1.0) · ICashSessionRepository.cs
├── Marketjoya.Modules.CashRegister.Application/CashSessions/
│   OpenCashSession/ · ApproveCashSessionOpening/ (1.1.0) · RejectCashSessionOpening/ (1.1.0)
│   GetCashSession/ (1.1.0) · GetPendingCashSessionApprovals/ (1.1.0) · Events/
├── Marketjoya.Modules.CashRegister.Infrastructure/
│   CashRegisterDbContext.cs · CashRegisterModule.cs · CashRegisterOptions.cs (1.1.0) · CashSessions/ · Migrations/
└── Marketjoya.Modules.CashRegister.Presentation/CashSessions/
    OpenCashSession/ · ApproveCashSessionOpening/ · RejectCashSessionOpening/ · GetCashSession/ · GetPendingCashSessionApprovals/
```

- `OpenCashSessionCommand(Guid ClientMutationId, decimal OpeningAmount) : ICommand, IRequest<Result<OpenCashSessionResponse>>`. La caja sale de `ICurrentUser.CashRegisterId`, nunca del body (BR-CASH-001-03).
- `OpenCashSessionCommandValidator`: `OpeningAmount` `InclusiveBetween(0m, 5000m)` y `PrecisionScale(6, 2, true)` con `.WithErrorCode("OpeningAmount.Invalid")`; `ClientMutationId` `NotEmpty` con `.WithErrorCode("ClientMutationId.Required")`.
- `OpenCashSessionCommandHandler`, en orden:
  1. `CashRegisterId` nulo → `CashSessionErrors.RegisterNotAssigned`.
  2. `GetByClientMutationIdAsync` → si existe, `Result.Success` con la apertura ya registrada (BR-CASH-001-04).
  3. `ExistsActiveForRegisterAsync` → `CashSessionErrors.AlreadyOpen` (BR-CASH-001-01).
  4. `CashSession.Open(..., approvalThreshold)` + `AddAsync`; el commit lo hace `TransactionBehavior`.
- `CashSession : AggregateRoot<CashSessionId>`:
  - `Open` verifica `OpeningAmountMustBeInRangeRule`; con monto ≤ umbral queda `Open` y emite `CashSessionOpenedDomainEvent`.
  - 1.1.0: con monto > umbral y `ApprovalThresholdEnabled` queda `PendingApproval` y emite `CashSessionApprovalRequestedDomainEvent` (BR-CASH-001-06); no hay evento de integración.
  - 1.1.0: `Approve(supervisorId, at)` verifica `SupervisorMustDifferFromCashierRule` (BR-CASH-001-07), pasa a `Open` con `DecidedBy`/`DecidedAt` y emite `CashSessionOpenedDomainEvent` con `OpenedAt = at`.
  - 1.1.0: `Reject(supervisorId, at)` verifica la misma regla y pasa a `Rejected`. Si el estado no es `PendingApproval` → `CashSessionErrors.NotPendingApproval`.
- `ICashSessionRepository` (4 métodos, sin cambios en 1.1.0): `AddAsync`, `GetByIdAsync`, `GetByClientMutationIdAsync`, `ExistsActiveForRegisterAsync`.
- 1.1.0 — `ApproveCashSessionOpeningCommand(Guid CashSessionId)` y `RejectCashSessionOpeningCommand(Guid CashSessionId)`: `GetByIdAsync` filtrado por `ICurrentUser.CompanyIds` → `CashSessionErrors.NotFound`.
- 1.1.0 — `GetCashSessionQuery(Guid CashSessionId)` y `GetPendingCashSessionApprovalsQuery()` con Dapper sobre `ICashRegisterReadDbConnection`.
- 1.1.0 — `CashRegisterOptions`: `ApprovalThreshold = 500.00m`, `ApprovalThresholdEnabled` (configuración `CashRegister:*`).
- `CashSessionOpenedDomainEventHandler : IDomainEventHandler<CashSessionOpenedDomainEvent>` publica el evento de integración y emite la métrica.
- Endpoints (todos con `.WithStandardProblems()`):

| Ruta | Permiso | `WithName` → operationId |
|---|---|---|
| `POST /cash-sessions` | `cash-sessions.open` | `OpenCashSession` → `CashRegister_OpenCashSession` |
| `GET /cash-sessions/{cashSessionId:guid}` (1.1.0) | `cash-sessions.open` | `GetCashSession` → `CashRegister_GetCashSession` |
| `GET /cash-sessions/pending-approvals` (1.1.0) | `cash-sessions.approve` | `GetPendingCashSessionApprovals` → `CashRegister_GetPendingCashSessionApprovals` |
| `POST /cash-sessions/{cashSessionId:guid}/approval` (1.1.0) | `cash-sessions.approve` | `ApproveCashSessionOpening` → `CashRegister_ApproveCashSessionOpening` |
| `POST /cash-sessions/{cashSessionId:guid}/rejection` (1.1.0) | `cash-sessions.approve` | `RejectCashSessionOpening` → `CashRegister_RejectCashSessionOpening` |

```csharp
public sealed class OpenCashSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/cash-sessions", async (OpenCashSessionRequest request, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(request.ToCommand(), cancellationToken)).ToHttpResult())
            .RequireAuthorization("cash-sessions.open")
            .WithName("OpenCashSession")
            .WithSummary("Opens the cash session of the caller's cash register.")
            .Produces<OpenCashSessionResponse>()
            .WithStandardProblems();
}
```

- `OpenCashSessionResponse(Guid CashSessionId, decimal OpeningAmount, bool RequiresApproval, DateTimeOffset? OpenedAt)`; `RequiresApproval` se agrega en 1.1.0. El replay idempotente devuelve la misma respuesta con 200.

## Frontend

Versión 1.0.0: No aplica. Desde 1.1.0 (CHG-001), feature hexaclean `cash-register`:

- `src/core/cash-register/port/in/`: `get-pending-cash-session-approvals.port.ts`, `approve-cash-session-opening.port.ts`, `reject-cash-session-opening.port.ts`.
- `src/core/cash-register/port/out/cash-session-approvals.port.ts` (puerto por capacidad, no CRUD).
- `src/core/cash-register/application/use-case/`: un caso de uso por puerto de entrada, con `GoResult<…, AppError>`.
- `src/data/cash-register/pending-cash-session-approval.dto.ts`.
- `src/infrastructure/http/cash-register/cash-session-approvals.http-adapter.ts`: único traductor ProblemDetails → `AppError` con `toAppError`.
- `src/ui/cash-register/pending-approvals/pending-approvals.screen.ts` con facade y store; `MrCard`, `MrMoney`, `MrStatusChip`, `MrButton`, `MrDialog` para confirmar. Ramifica por `type` y luego `code`: `CashSession.NotPendingApproval` y `Concurrency.Conflict` recargan la lista y muestran "Esta apertura ya fue decidida"; `CashSession.SelfApprovalNotAllowed` muestra su mensaje.
- Ruta `cash-register/pending-approvals` en `src/ui/app.routes.ts`, oculta sin el permiso `cash-sessions.approve` (la autorización real la hace el backend). `data-testid`: `pending-approval-row`, `approve-opening`, `reject-opening`, `confirm-decision`.

## Mobile

- `OpenCashSessionUseCase(gateway: CashSessionGateway, localStore: CashSessionLocalStore, outbox: PendingSyncWriter, connectivity: ConnectivityMonitor)` en `domain/cashregister`:
  - Recibe el `ClientMutationId` del ViewModel (uno por intento de apertura; ver BUG-001) y genera el `LocalId`.
  - Con conexión: `gateway.open(...)` → `AppResult<CashSessionOpening>`; si `requiresApproval`, guarda el estado local `PendingApproval` (1.1.0).
  - Sin conexión, o `AppError.type == NETWORK`, con monto ≤ S/ 500.00: guarda la apertura local `PendingSync` y hace `outbox.enqueue(PendingSyncOperation(kind = "OpenCashSessionCommand"))` en una transacción Room (BR-CASH-001-05).
  - 1.1.0: sin conexión y monto > S/ 500.00: `AppResult.Failure(AppError(type = VALIDATION, code = "OpeningAmount.RequiresConnection"))` local, sin encolar (BR-CASH-001-08).
- 1.1.0 — `ObserveCashSessionDecisionUseCase`: consulta `gateway.get(cashSessionId)` cada 5 s con `DispatcherProvider` mientras la pantalla está visible; `Open` habilita la venta, `Rejected` muestra "El supervisor rechazó la apertura".
- `CashSessionHttpGateway` (`core/network`): errores solo vía `HttpErrorMapper`; ramifica por `type` y luego `code`.
- Envío posterior: el worker de sync aplica `SyncAttemptPolicy`; el payload se lee de la tabla local `cash_sessions` por `client_mutation_id` (la fila del outbox solo guarda `kind`). En Market Real el worker sigue pendiente de decisión; el ejemplo lo da por disponible (ASM-CASH-001-01).
- `OpenCashSessionViewModel` + `OpenCashSessionScreen` (Compose, `MrTextField`, `MrMoney`, `MrButton`, `MrStatusChip`); `testTag`: `cash-opening-amount`, `cash-opening-submit`, `cash-opening-status`. Textos por `code` en `strings.xml`.

## Base de datos y migraciones

- Schema `cash_register`, tabla `cash_sessions`: `id uuid pk`, `cash_register_id uuid not null`, `company_id uuid not null`, `cashier_user_id uuid not null`, `opening_amount numeric(9,2) not null`, `currency char(3) not null default 'PEN'`, `status text not null`, `client_mutation_id uuid not null`, `opened_at timestamptz`, más las columnas sombra `created_at`, `created_by`, `updated_at`, `updated_by`, `sync_version`.
- Índices: `ux_cash_sessions_active_register` único parcial `(cash_register_id)`; `ux_cash_sessions_client_mutation_id` único `(client_mutation_id)`.
- 1.0.0 — Migración EF `CreateCashSessions` con predicado `WHERE status = 'Open'`: `dotnet ef migrations add CreateCashSessions --project src/Modules/CashRegister/Marketjoya.Modules.CashRegister.Infrastructure --startup-project src/Api/Marketjoya.Api --context CashRegisterDbContext`.
- 1.1.0 — Migración EF `AddCashSessionApproval` (expand): agrega `requested_at timestamptz not null default now()`, `decided_by uuid`, `decided_at timestamptz`; `opened_at` admite nulos; recrea `ux_cash_sessions_active_register` con `WHERE status IN ('Open', 'PendingApproval')`; índice `ix_cash_sessions_pending (company_id) WHERE status = 'PendingApproval'`. Sin backfill: las filas existentes son `Open`.
- Mobile: `MarketjoyaDatabase` versión 3 con `MIGRATION_2_3` (tabla local `cash_sessions`); 1.1.0 versión 4 con `MIGRATION_3_4` (columna `requires_approval`); cada una con su caso en `MarketjoyaMigrationsTest`.

## Contratos

- `openapi:CashRegister_OpenCashSession` — `POST /api/v1/cash-sessions`. Request `{ "clientMutationId": "uuid", "openingAmount": 200.00 }`; 200 `OpenCashSessionResponse`; 400/409/500 ProblemDetails; 401/403 por autorización. Consumidor: mobile. 1.1.0 agrega `requiresApproval` a la respuesta (aditivo).
- 1.1.0 — `openapi:CashRegister_GetCashSession` (mobile), `openapi:CashRegister_GetPendingCashSessionApprovals`, `openapi:CashRegister_ApproveCashSessionOpening` y `openapi:CashRegister_RejectCashSessionOpening` (front). Approval y rejection sin body; 204 en éxito.
- `client_mutation_id` viaja en el body. En Market Real el transporte sigue pendiente de decisión; ADR-001 lo fija solo para este ejemplo.
- Las respuestas no exponen el estado como enum numérico o texto (la serialización de enums sigue pendiente de decisión): se usan booleanos (`requiresApproval`, `isOpen`, `isRejected`) en `GetCashSessionResponse`.

## Eventos y mensajería

- Dominio: `CashSessionOpenedDomainEvent(CashSessionId, CashRegisterId, CompanyId, CashierUserId, OpeningAmount, OpenedAt)`; 1.1.0 `CashSessionApprovalRequestedDomainEvent` (solo métrica, sin integración).
- Integración `event:CashSessionOpenedIntegrationEvent`, en `Marketjoya.BuildingBlocks.Contracts/CashRegister/` (versión 1, sin cambios en 1.1.0):

```csharp
public sealed record CashSessionOpenedIntegrationEvent(
    Guid Id, DateTimeOffset OccurredOn, Guid CashSessionId, Guid CashRegisterId, Guid CompanyId,
    Guid CashierUserId, decimal OpeningAmount, string Currency, DateTimeOffset OpenedAt)
    : IntegrationEvent(Id, OccurredOn);
```

- Exchange `marketjoya.CashSessionOpenedIntegrationEvent`; cola del consumidor `marketjoya.Reports.CashSessionOpenedConsumer` (REPORT-FEAT-001).
- Se escribe en el outbox dentro de la transacción del command; un rollback lo descarta. Un replay idempotente no emite de nuevo.
- 1.1.0: se publica al abrir directo **o** al aprobar (`OpenedAt` = hora de aprobación); nunca en `PendingApproval` ni al rechazar. Esto induce el cambio de REPORT-FEAT-001 (CHG-001).

## Integraciones externas

No aplica — la apertura y la aprobación no llaman a SUNAT, OCR ni GPS; la impresión queda fuera (Q-CASH-001-02).

## Seguridad y autorización

- `RequireAuthorization("cash-sessions.open")`: sin token 401, sin permiso 403.
- 1.1.0 — `RequireAuthorization("cash-sessions.approve")` para las operaciones del supervisor; los datos se filtran por `company` del token (otra tienda → 404 `CashSession.NotFound`).
- 1.1.0 — La autoaprobación se impide en dominio (`SupervisorMustDifferFromCashierRule` con `ICurrentUser.UserId`), no solo en la UI.
- La caja y la empresa salen de los claims (`cash_register`, `company`); el body no puede elegir caja (BR-CASH-001-03).
- Auditoría (REQ-007): `AuditableInterceptor` llena `created_by`/`created_at`; la fila guarda `cashier_user_id`, `opened_at` y, desde 1.1.0, `decided_by`/`decided_at`.
- Mobile: la apertura local no guarda tokens; el envío posterior usa la sesión vigente.

## Observabilidad

- Trazas `marketjoya.usecase.CashRegister.OpenCashSession` y, en 1.1.0, `marketjoya.usecase.CashRegister.ApproveCashSessionOpening` y `marketjoya.usecase.CashRegister.RejectCashSessionOpening`, con `marketjoya.error.code` y `marketjoya.error.type` en fallas.
- Métricas: `marketjoya_cash_register_sessions_opened_total` (labels `company`, `register`); 1.1.0 `marketjoya_cash_register_openings_pending_approval_total` y `marketjoya_cash_register_opening_decisions_total` (label `decision`: `approved`/`rejected`).
- `X-Correlation-Id` generado por el POS y por la consola en cada request; llega al `CorrelationId` del evento.
- Se reutilizan las métricas previstas del outbox: `marketjoya_outbox_pending`, `marketjoya_sync_lag_seconds`.

## Errores

| code | type | HTTP | Origen |
|---|---|---|---|
| OpenCashSession.Validation | VALIDATION | 400 | `ValidationBehavior`; `errors` con `openingAmount`/`OpeningAmount.Invalid` y `clientMutationId`/`ClientMutationId.Required` |
| CashSession.AlreadyOpen | CONFLICT | 409 | `CashSessionErrors.AlreadyOpen` o violación de `ux_cash_sessions_active_register` |
| CashSession.RegisterNotAssigned | CONFLICT | 409 | `CashSessionErrors.RegisterNotAssigned` |
| CashSession.NotFound (1.1.0) | NOT_FOUND | 404 | Apertura inexistente o de otra empresa |
| CashSession.NotPendingApproval (1.1.0) | CONFLICT | 409 | Aprobar o rechazar una apertura ya decidida |
| CashSession.SelfApprovalNotAllowed (1.1.0) | CONFLICT | 409 | `SupervisorMustDifferFromCashierRule` |
| Concurrency.Conflict | CONFLICT | 409 | Carrera con el mismo `client_mutation_id` (1.0.0) o entre supervisores por `sync_version` (1.1.0) |
| OpeningAmount.RequiresConnection (1.1.0) | VALIDATION | — | Solo local en el POS (BR-CASH-001-08); no viaja por HTTP |
| (sin `code`) | UNAUTHORIZED / FORBIDDEN | 401 / 403 | Framework; el cliente aplica `Auth.Unauthorized` / `Auth.Forbidden` |

## Concurrencia y transacciones

- Un command = una transacción: escritura en `cash_sessions` y outbox del evento se confirman juntos.
- Aperturas secuenciales: `ExistsActiveForRegisterAsync` responde `CashSession.AlreadyOpen`.
- Carrera entre dos dispositivos (AC-CASH-001-03): ambos pasan el chequeo; el índice único parcial rechaza el segundo `INSERT` al confirmar (SQLSTATE 23505) y `UniqueConstraintErrorMap` responde 409 `CashSession.AlreadyOpen`. Desde 1.1.0 el índice incluye `PendingApproval`: una apertura pendiente también ocupa la caja.
- Doble envío con el mismo `client_mutation_id` (AC-CASH-001-05): secuencial → replay; simultáneo → `Concurrency.Conflict` y el siguiente intento hace replay.
- 1.1.0 — Dos supervisores deciden la misma apertura: el segundo `UPDATE` falla por `sync_version` → 409 `Concurrency.Conflict`; la consola recarga y ve `CashSession.NotPendingApproval`.
- Mobile: apertura local + `enqueue` en una transacción Room; `enqueue` es no-op si el `ClientMutationId` ya existe.

## Compatibilidad y datos existentes

- 1.0.0: endpoint y tablas nuevos, sin datos que migrar.
- 1.1.0: la respuesta de apertura agrega un campo (compatible). Una app POS 1.0.0 no entiende `requiresApproval` y mostraría "abierta" una apertura pendiente: por eso `CashRegister:ApprovalThresholdEnabled` queda en `false` hasta que el 100 % de los POS tenga la app 1.1.0.
- 1.1.0: filas existentes quedan `Open` con `requested_at` = `created_at` (default); la migración es aditiva.
- Room 3 → 4 con migración explícita; el outbox existente se conserva.

## Despliegue y reversión

1. Bundle de migración del schema `cash_register` antes del rollout del API (1.1.0: `AddCashSessionApproval`).
2. API con `CashRegister:ApprovalThresholdEnabled=false`.
3. Front con la pantalla de aprobaciones (vacía mientras el umbral esté apagado).
4. App POS 1.1.0 con despliegue escalonado (10 % → 100 %).
5. Con el 100 % de POS en 1.1.0, activar `CashRegister:ApprovalThresholdEnabled=true`.

Reversión: apagar `ApprovalThresholdEnabled` (las pendientes existentes se deciden desde la consola); retirar versiones de app o front si hace falta. Las migraciones son aditivas y no requieren revertirse.

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Sesiones duplicadas por reintentos o dos dispositivos | media | alto | Índice único parcial + idempotencia (ADR-001); TEST-CASH-001-05 y TEST-CASH-001-09 |
| Aperturas sin conexión rechazadas al sincronizar | baja | medio | Estado "Rechazada" visible; métrica `marketjoya_sync_conflicts_total` |
| Consumer de Reports caído | baja | bajo | Outbox/inbox, reintentos y cola de error de Wolverine |
| 1.1.0: POS 1.0.0 con el umbral activo | media | alto | Configuración `ApprovalThresholdEnabled` activada solo con 100 % de POS en 1.1.0 |
| 1.1.0: cajas bloqueadas por aperturas pendientes sin decidir | media | medio | Métrica de pendientes y alerta si una apertura lleva más de 15 minutos pendiente |

## Análisis de impacto

- Módulos directos: CASH. Indirectos: AUTH (claim `cash_register`, permisos `cash-sessions.open` y `cash-sessions.approve`) y REPORT (consume el evento).
- Consumidores de los datos: REPORT-FEAT-001, con read model propio; nadie lee el schema `cash_register`.
- Endpoints compartidos: `CashRegister_OpenCashSession` cambia de forma aditiva (mobile); cuatro operaciones nuevas (mobile y front).
- Tablas y schemas compartidos: No aplica — schema propio del módulo.
- Eventos: `CashSessionOpenedIntegrationEvent` sin cambio de forma; cambia el momento de publicación para sesiones aprobadas → REPORT-FEAT-001 en `features_affected` y cambio inducido en CHG-001.
- Procesos programados: sincronización offline del POS; 1.1.0 consulta periódica de la decisión mientras la pantalla está visible.
- Permisos: `cash-sessions.open` (cajero) y, desde 1.1.0, `cash-sessions.approve` (supervisor).
- Reportes: REPORT-FEAT-001 (hora de apertura de sesiones aprobadas).
- Notificaciones: No aplica — no se notifica a nadie.
- Integraciones externas: No aplica.
- Regresión: TEST-AUTH-001-01 (sesión con caja), TEST-REPORT-001-01 (consumer), TEST-REPORT-001-03 (reporte vía API).
- Compatibilidad: cambio aditivo en `/api/v1`; POS 1.0.0 protegidos con configuración; Room 4.
- Contrato de errores: `code` nuevos `CashSession.AlreadyOpen`, `CashSession.RegisterNotAssigned`, `OpeningAmount.Invalid`, `ClientMutationId.Required` (1.0.0) y `CashSession.NotFound`, `CashSession.NotPendingApproval`, `CashSession.SelfApprovalNotAllowed`, `OpeningAmount.RequiresConnection` (1.1.0).
- Rendimiento y seguridad: índices parciales pequeños; p95 < 2 s (REQ-006) medido en TEST-CASH-001-15; autoaprobación bloqueada en dominio.

## Archivos a crear o modificar

- backend:
  - `src/Modules/CashRegister/Marketjoya.Modules.CashRegister.Domain/CashSessions/CashSession.cs`, `OpeningAmount.cs`, `CashSessionErrors.cs`, `CashSessionOpenedDomainEvent.cs`, `ICashSessionRepository.cs`; 1.1.0: `CashSessionApprovalRequestedDomainEvent.cs`, `SupervisorMustDifferFromCashierRule.cs`
  - `src/Modules/CashRegister/Marketjoya.Modules.CashRegister.Application/CashSessions/OpenCashSession/*`; 1.1.0: `ApproveCashSessionOpening/*`, `RejectCashSessionOpening/*`, `GetCashSession/*`, `GetPendingCashSessionApprovals/*`
  - `src/Modules/CashRegister/Marketjoya.Modules.CashRegister.Application/CashSessions/Events/CashSessionOpenedDomainEventHandler.cs`
  - `src/Modules/CashRegister/Marketjoya.Modules.CashRegister.Infrastructure/CashRegisterDbContext.cs`, `CashRegisterModule.cs`, `CashSessions/CashSessionConfiguration.cs`, `CashSessions/CashSessionRepository.cs`, `Migrations/<timestamp>_CreateCashSessions.cs`; 1.1.0: `CashRegisterOptions.cs`, `CashSessions/CashRegisterReadDbConnection.cs`, `Migrations/<timestamp>_AddCashSessionApproval.cs`
  - `src/Modules/CashRegister/Marketjoya.Modules.CashRegister.Presentation/CashSessions/OpenCashSession/*`; 1.1.0: `ApproveCashSessionOpening/*`, `RejectCashSessionOpening/*`, `GetCashSession/*`, `GetPendingCashSessionApprovals/*`
  - `src/BuildingBlocks/Marketjoya.BuildingBlocks.Contracts/CashRegister/CashSessionOpenedIntegrationEvent.cs`
  - `src/Common/Marketjoya.Common.Infrastructure/Persistence/UniqueConstraintErrorMap.cs`
  - `openapi/marketjoya-api-v1.json`
- mobile:
  - `domain/src/main/kotlin/pe/marketjoya/domain/cashregister/OpenCashSessionUseCase.kt`, `CashSessionGateway.kt`, `CashSessionLocalStore.kt`, `CashSessionOpening.kt`; 1.1.0: `ObserveCashSessionDecisionUseCase.kt`
  - `core/network/src/main/kotlin/pe/marketjoya/core/network/cashregister/CashSessionHttpGateway.kt`
  - `core/database/src/main/kotlin/pe/marketjoya/core/database/cashregister/CashSessionEntity.kt`, `CashSessionDao.kt`, `RoomCashSessionLocalStore.kt`; `MarketjoyaMigrations.kt` (`MIGRATION_2_3`; 1.1.0 `MIGRATION_3_4`)
  - `feature/cash-register/src/main/kotlin/pe/marketjoya/feature/cashregister/OpenCashSessionViewModel.kt`, `OpenCashSessionScreen.kt`
  - `maestro/cash-register/open-cash-session.yaml`
- front (1.1.0):
  - `src/core/cash-register/port/in/*.port.ts`, `src/core/cash-register/port/out/cash-session-approvals.port.ts`, `src/core/cash-register/application/use-case/*.use-case.ts`
  - `src/data/cash-register/pending-cash-session-approval.dto.ts`
  - `src/infrastructure/http/cash-register/cash-session-approvals.http-adapter.ts`
  - `src/ui/cash-register/pending-approvals/pending-approvals.screen.ts`, `src/ui/app.routes.ts`
  - `e2e/cash-register/approve-cash-session-opening.spec.ts`

## Orden de implementación

1. backend — contrato: `CashSessionOpenedIntegrationEvent` y forma del endpoint revisada con mobile.
2. backend — dominio `CashSession` y reglas (TEST-CASH-001-01, TEST-CASH-001-02).
3. backend — migración, repositorio, índices y `UniqueConstraintErrorMap` (TEST-CASH-001-05, TEST-CASH-001-10).
4. backend — command, validador y handler idempotente (TEST-CASH-001-03, TEST-CASH-001-04, TEST-CASH-001-09).
5. backend — evento de dominio → integración (TEST-CASH-001-08).
6. backend — endpoint y OpenAPI (TEST-CASH-001-06, TEST-CASH-001-07).
7. mobile — gateway y mapeo de errores (TEST-CASH-001-11).
8. mobile — caso de uso sin conexión y Room 3 (TEST-CASH-001-12, TEST-CASH-001-13).
9. mobile — pantalla y flujo Maestro (TEST-CASH-001-14, TEST-CASH-001-16).
10. QA — medición en POS (TEST-CASH-001-15) y regresión de TEST-AUTH-001-01.
11. 1.1.0 backend — contrato OpenAPI de las cuatro operaciones nuevas revisado con front y mobile.
12. 1.1.0 backend — dominio `PendingApproval`, `Approve`, `Reject` y regla de autoaprobación (TEST-CASH-001-17).
13. 1.1.0 backend — migración `AddCashSessionApproval` (TEST-CASH-001-24, TEST-CASH-001-05 modificada).
14. 1.1.0 backend — commands, queries, endpoints y configuración (TEST-CASH-001-18, TEST-CASH-001-19, TEST-CASH-001-20, TEST-CASH-001-21).
15. 1.1.0 front — adapter, casos de uso y pantalla de aprobaciones (TEST-CASH-001-22).
16. 1.1.0 mobile — pendiente de aprobación, consulta de la decisión y bloqueo sin conexión (TEST-CASH-001-23, TEST-CASH-001-14 modificada).
17. 1.1.0 QA — regresión TEST-AUTH-001-01, TEST-REPORT-001-01 y TEST-REPORT-001-03; activación de la configuración en QA.
