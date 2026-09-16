# Common — Shared Kernel por capas

Common respeta Clean Architecture: 4 proyectos, mismas reglas de dependencia que los módulos. Nada de lógica de negocio: solo mecanismos transversales. Rutas relativas a `MarketjoyaBackend/src/Common/`.

## 1. Marketjoya.Common.Domain

```text
Marketjoya.Common.Domain/
├── Entities/
│   ├── Entity.cs                 ← Entity<TId>: Id, igualdad por tipo + id (transitorias nunca iguales), CheckRule(rule)
│   ├── AggregateRoot.cs          ← AggregateRoot<TId>: RaiseDomainEvent, GetDomainEvents, ClearDomainEvents
│   ├── IAggregateRoot.cs
│   └── States.cs
├── ValueObjects/ValueObject.cs   ← igualdad por GetEqualityComponents()
├── DomainEvents/
│   ├── IDomainEvent.cs           ← OccurredOn
│   ├── DomainEvent.cs            ← abstract record DomainEvent(DateTimeOffset OccurredOn)
│   └── IDomainEventHandler.cs    ← Handle(TDomainEvent, CancellationToken)
├── Rules/
│   ├── IBusinessRule.cs          ← Message + IsBroken()
│   └── BusinessRuleValidationException.cs
├── Results/
│   ├── Result.cs / ResultT.cs    ← Success(), Success(value), Failure(error), Failure<T>(error)
│   ├── Error.cs                  ← record Error(Code, Description, Type) + Error.None + fábricas
│   └── ErrorType.cs              ← Failure, Validation, NotFound, Conflict
├── PagedResult.cs                ← PagedResult<T>(Items, TotalCount, PageNumber, PageSize)
└── Extensions/DateTimeExtensions.cs
```

- Todo agregado hereda `AggregateRoot<TId>`; toda regla implementa `IBusinessRule` y se verifica con `CheckRule(...)` (heredado de `Entity<TId>`).
- Flujo controlado → `Result.Failure(Error)`. Invariante rota → `BusinessRuleValidationException` (→ 409). Ver `references/errors-and-results.md`.

## 2. Marketjoya.Common.Application — mediador propio

```text
Marketjoya.Common.Application/
├── Messaging/
│   ├── ISender.cs                ← Send<TResponse>(IRequest<TResponse>) where TResponse : Result
│   ├── IPublisher.cs             ← Publish(INotification) y Publish(IDomainEvent)
│   ├── IRequest.cs / IRequestT.cs← IRequest = IRequest<Result>; IRequest<TResponse : Result>
│   ├── ICommand.cs               ← marcador: fuerza TransactionBehavior
│   ├── IRequestHandler.cs
│   ├── INotification.cs / INotificationHandler.cs
│   ├── IPipelineBehavior.cs
│   └── Mediator.cs               ← internal
├── Behaviors/                    ← Logging, Telemetry, Validation, Transaction, UseCaseDiagnostics
├── Abstractions/
│   ├── IClock.cs                 ← UtcNow, LimaNow
│   ├── ICurrentUser.cs           ← UserId, CompanyIds, WarehouseIds, CashRegisterId
│   ├── ISqlConnectionFactory.cs  ← CreateConnection(), OpenConnectionAsync()
│   ├── ITransactionManager.cs    ← BeginTransactionAsync, CommitAsync, RollbackAsync
│   ├── IIntegrationEventPublisher.cs
│   └── ICorrelationIdAccessor.cs ← HeaderName = "X-Correlation-Id", CorrelationId, Set()
├── DependencyInjection/ApplicationDependencyInjection.cs
└── Time/Clock.cs
```

### Convenciones del mediador

- `AddCommonApplication(params Assembly[])`: registra `ISender`/`IPublisher` (scoped), los behaviors en orden `Logging → Telemetry → Validation → Transaction`, `TimeProvider` + `IClock`, y desde los ensamblados dados los `IRequestHandler`, `INotificationHandler`, `IDomainEventHandler` y validators FluentValidation (incluidos `internal`). El host lo llama sin ensamblados; cada `IModule.Register` lo llama con el ensamblado Application del módulo.
- Commands: `IRequest` o `IRequest<Result<T>>`; pasan por `TransactionBehavior` si implementan `ICommand` o su nombre termina en `Command`.
- Queries: `IRequest<Result<TResponse>>`, nombre `*Query`; sin transacción.
- `ValidationBehavior`: si hay fallos devuelve `Error.Validation("<UseCase>.Validation", "La solicitud tiene errores de validación.", fieldErrors)` con un `FieldError` por fallo (código de `WithErrorCode`, o `Validation.<Regla>` / `Validation.Custom`) sin invocar el handler (`references/errors-and-results.md`, Validación).
- `TransactionBehavior`: `Begin` → handler → `Commit` si `IsSuccess`, `Rollback` si failure; si el handler lanza, `Rollback` y relanza.
- Eventos de dominio: `IPublisher.Publish(IDomainEvent)` los entrega a `IDomainEventHandler<T>` (Application del mismo módulo), dentro de la transacción.
- `ITransactionManager` solo lo usa `TransactionBehavior`. No existe `IUnitOfWork`.

Wolverine no reemplaza al mediador. Ningún handler de Application referencia Wolverine.

## 3. Marketjoya.Common.Infrastructure

```text
Marketjoya.Common.Infrastructure/
├── Authentication/     AddCommonAuthentication, CustomClaims, JwtOptions, políticas de permiso, UserContext
├── Health/             AddCommonHealthChecks, RabbitMqHealthCheck
├── Messaging/          AddCommonMessaging, MessagingOptions, IntegrationEventNaming, MessagingDiagnostics,
│                       Consumers/IIntegrationEventConsumer, Correlation/, Outbox/, DeadLetters/
├── Modules/IModule.cs  ← Name + Register(services, configuration)
├── Mongodb/            AuditCollections (audit_events, dead_letters), AuditStoreOptions
├── Persistence/        AddCommonPersistence, AddModuleDbContext, AddReadDbConnection, BaseDbContext,
│                       ModuleDbContextOptions, ShadowProperties, TransactionManager, ReadDbConnection,
│                       NpgsqlSqlConnectionFactory, DbUpdateConcurrencyExceptionHandler,
│                       Interceptors/ (DomainEvents, Auditable, SyncVersion), TypeHandlers/ (DateOnly, DateTimeOffset)
├── Redis/              AddCommonRedis
└── Telemetry/          AddCommonTelemetry
```

### Base de datos y transacciones

- Una sola cadena `ConnectionStrings:Database` y un `NpgsqlDataSource`. Un schema por módulo (`BaseDbContext.Schema` → `HasDefaultSchema`). Nombres snake_case (`UseSnakeCaseNamingConvention`). `__EFMigrationsHistory` dentro del schema del módulo.
- `AddCommonPersistence(configuration)` (una vez, en el host): data source, `ISqlConnectionFactory`, `ITransactionManager` scoped, interceptores y type handlers Dapper (`DateOnly`, `DateTimeOffset` sobre `timestamptz`).
- `AddModuleDbContext<TContext>(schema)`: el contexto usa la conexión del `TransactionManager` del scope (una conexión Npgsql por scope compartida por todos los DbContext de módulos), se enlista en su transacción y registra los interceptores en orden DomainEvents → Auditable → SyncVersion.
- `CommitAsync`: `SaveChangesAsync` de cada contexto enlistado y luego commit; cualquier fallo hace rollback. Tras commit/rollback notifica a los participantes (outbox: flush o descarte). Rollback limpia el change tracker.
- `DomainEventsInterceptor` despacha los eventos en `SavingChangesAsync`, antes de escribir filas y antes del commit, hasta vaciarlos. `SaveChanges` síncrono lanza `NotSupportedException`.
- Estrategia de reintentos de EF deshabilitada a propósito: `NpgsqlRetryingExecutionStrategy` rechaza las transacciones explícitas de `TransactionBehavior` y repetir un comando re-ejecutaría handlers de eventos. Los fallos transitorios salen como excepción; reintentan los llamadores idempotentes (`client_mutation_id`) y Wolverine.
- Shadow properties: toda tabla raíz lleva `created_at`, `created_by`, `updated_at`, `updated_by` (llenadas por `AuditableInterceptor` desde `IClock` e `ICurrentUser`); todo aggregate root lleva `sync_version` (token de concurrencia: 1 al insertar, +1 al actualizar). Nombres reservados: ninguna entidad declara `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `SyncVersion`.
- Escritura con `sync_version` desactualizado → `DbUpdateConcurrencyException` → 409 `Concurrency.Conflict` (`AddPersistenceExceptionHandling`, en el host).
- Lectura: `ReadDbConnection` (conexión perezosa por scope; Dapper abre y cierra) + `AddReadDbConnection<I<Modulo>ReadDbConnection, <Modulo>ReadDbConnection>()`.
- Migraciones: `dotnet ef migrations add <Name> --project src/Modules/<M>/Marketjoya.Modules.<M>.Infrastructure --startup-project src/Api/Marketjoya.Api --context <M>DbContext`. Se despliegan como bundles antes del rollout; la API nunca llama `Database.Migrate()` al arrancar. Expand/contract. Detalle: `MarketjoyaBackend/docs/migrations.md`.

### Wolverine (integración)

Configuración, outbox, retries y DLQ: `references/events.md`.

## 4. Marketjoya.Common.Presentation

```text
Marketjoya.Common.Presentation/
├── DependencyInjection/   AddCommonPresentation (ProblemDetails + enricher + handler de reglas), UseCorrelationId
├── Endpoints/             IEndpoint, EndpointExtensions (AddEndpoints, AddEndpointVersioning, MapEndpoints),
│                          ApiVersions, EndpointProblemExtensions (WithStandardProblems), metadatos de módulo/acceso
├── Middlewares/           BusinessRuleValidationExceptionHandler, CorrelationIdMiddleware
└── Results/               ResultExtensions.ToHttpResult, ErrorProblem
```

Todo endpoint traduce el `Result` con `ToHttpResult()`. Ninguno construye `ProblemDetails` a mano. Contrato HTTP: `references/errors-and-results.md` y `templates/endpoint.md`.

## 5. Host (Marketjoya.Api)

Registro (`AddApiHost`): `AddCommonApplication()` → `AddCommonPersistence` → `IModule.Register` de cada módulo → `AddCommonMessaging` (consumers de los ensamblados de cada `IModule`) → Redis → autenticación → health checks → telemetría → `AddCommonPresentation` → `AddPersistenceExceptionHandling` → `AddEndpoints(ensamblados Presentation)` → CORS → forwarded headers → request timeouts (30 s) → documentación OpenAPI.

Pipeline (`UseApiPipeline`): `ForwardedHeaders → ExceptionHandler → StatusCodePages → CorrelationId → Routing → CORS → Authentication → Authorization → RequestTimeouts → health checks → MapEndpoints → documentación (solo Development)`.

Claves de configuración y superficie HTTP: `MarketjoyaBackend/docs/configuration.md`.
