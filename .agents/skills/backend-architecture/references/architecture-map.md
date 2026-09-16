# Mapa de arquitectura

Monolito modular .NET 10. Un host; cada módulo son 4 proyectos. Common también va por capas. Todo vive bajo `MarketjoyaBackend/`.

## Solución

```text
src/
├── Api/
│   └── Marketjoya.Api/                           host: Program, Hosting/ModuleCatalog, OpenApi/
├── BuildingBlocks/
│   └── Marketjoya.BuildingBlocks.Contracts/      IIntegrationEvent, IntegrationEvent (solo BCL)
├── Common/
│   ├── Marketjoya.Common.Domain/                 Entity, AggregateRoot, ValueObject,
│   │                                             IDomainEvent, IBusinessRule, Result/Error
│   ├── Marketjoya.Common.Application/            mediador propio + behaviors + abstracciones
│   │                                             (IClock, ICurrentUser, ITransactionManager, ...)
│   ├── Marketjoya.Common.Infrastructure/         persistencia, Wolverine, auth, telemetría, IModule
│   └── Marketjoya.Common.Presentation/           IEndpoint, versionado, Result→IResult, ProblemDetails
└── Modules/
    └── <Modulo>/
        ├── Marketjoya.Modules.<Modulo>.Domain/
        ├── Marketjoya.Modules.<Modulo>.Application/
        ├── Marketjoya.Modules.<Modulo>.Infrastructure/
        └── Marketjoya.Modules.<Modulo>.Presentation/

tests/                                            ver ../../backend-testing/references/strategy.md
openapi/marketjoya-api-v1.json                    contrato commiteado (generado en build)
```

`SolutionDiscoveryTests` exige este esquema de nombres y carpetas, y que todo proyecto esté en `MarketjoyaBackend.sln`. Detalle de Common: `references/common-shared-kernel.md`.

## Capas de un módulo

| Proyecto | Responsabilidad | Prohibiciones principales |
|---|---|---|
| Domain | Agregados, VOs, eventos de dominio, reglas, puertos de lenguaje | Cualquier paquete; solo BCL + `Common.Domain` |
| Application | Casos de uso verticales + `Abstractions/` | EF Core, Npgsql, Wolverine, ASP.NET, Infrastructure/Presentation |
| Infrastructure | `<Modulo>Module`, DbContext, configs EF, repositorios, read connections, consumers | Lógica de negocio, referenciar Presentation u otros módulos |
| Presentation | Endpoint + request por caso de uso | Lógica de negocio, Dapper, `System.Data`, puertos de datos, Infrastructure |

## Vertical slice

Las cuatro capas se organizan por submódulo. Application y Presentation bajan a una carpeta por caso de uso. Si agregas `CreateUser`, sus piezas aparecen en la misma rebanada:

```text
Modules/Users/
├── Domain/Users/                        ← agregado, VOs, eventos, reglas, puerto
├── Application/
│   ├── Abstractions/                    ← puertos del módulo (p. ej. IUsersReadDbConnection)
│   └── Users/
│       ├── CreateUser/
│       │   ├── CreateUserCommand.cs
│       │   ├── CreateUserCommandHandler.cs
│       │   └── CreateUserCommandValidator.cs
│       ├── GetUserById/
│       │   ├── GetUserByIdQuery.cs
│       │   ├── GetUserByIdQueryHandler.cs
│       │   ├── GetUserByIdResponse.cs
│       │   └── GetUserByIdMapper.cs
│       └── Shared/                      ← SOLO si hay DTO/mapper en ≥2 casos de uso
├── Infrastructure/
│   ├── UsersModule.cs                   ← IModule, en la raíz del proyecto
│   ├── Persistence/                     ← UsersDbContext + configurations
│   ├── Users/                           ← adaptadores del submódulo
│   │   ├── UserRepository.cs
│   │   └── UsersReadDbConnection.cs
│   └── Messaging/                       ← consumers de eventos de integración
└── Presentation/
    └── Users/
        ├── CreateUser/
        │   ├── CreateUserEndpoint.cs
        │   └── CreateUserRequest.cs
        └── GetUserById/
            └── GetUserByIdEndpoint.cs
```

Reglas (verificadas por `Marketjoya.ArchitectureTests`):

- Handler `<Request>Handler` en `Application/<Submodulo>/<CasoDeUso>/`, mismo namespace que su request; nunca bajo `Abstractions/` ni `Shared/`.
- Endpoint `sealed <CasoDeUso>Endpoint` en `Presentation/<Submodulo>/<CasoDeUso>/`.
- `<Modulo>Module` en la raíz de Infrastructure; `<Modulo>DbContext` en `Infrastructure/Persistence`; consumers en `Infrastructure/Messaging`.
- Prohibidas carpetas horizontales `Handlers/`, `Services/`, `Managers/`, `Helpers/`, `Dtos/`, `Repositories/` en cualquier capa.
- `Shared/` solo dentro de un submódulo, nunca a nivel de capa.

## Flujo de una operación

```text
HTTP /api/v1/... → IEndpoint (grupo de MapEndpoints) → Request.ToCommand/Query → ISender.Send
    → Logging → Telemetry → Validation → Transaction (solo commands) → Handler
    → puerto (repositorio EF | read connection Dapper)
    ← Result → ToHttpResult()
```

- Facade in-process = mediador propio. Wolverine no entra en este camino.
- El handler de comando orquesta el agregado. El de query ejecuta SQL.
- `TransactionBehavior` solo envuelve commands: el request implementa `ICommand` o su nombre termina en `Command`.

## Composición del host

`Program` llama `builder.AddApiHost(ModuleCatalog.Modules, ModuleCatalog.EndpointAssemblies)` y `app.UseApiPipeline()`. `ModuleCatalog` (en `Marketjoya.Api/Hosting`) lista explícitamente cada `IModule` y cada ensamblado Presentation; hoy ambas listas están vacías. Detalle en `references/common-shared-kernel.md` (Host).

Recetas: `templates/command.md`, `templates/query.md`, `templates/endpoint.md`, `templates/module.md`.
