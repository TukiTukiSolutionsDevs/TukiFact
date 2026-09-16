# Reglas de dependencia

Verificadas por `MarketjoyaBackend/tests/Marketjoya.ArchitectureTests` (NetArchTest sobre tipos + lectura de los `.csproj`). `dotnet test` falla si se viola una regla; CI bloquea el merge. Tabla completa: `MarketjoyaBackend/docs/testing.md` (Architecture rules).

## Grafo de proyectos permitido

| Proyecto | Puede referenciar |
|---|---|
| `Marketjoya.Modules.X.Domain` | `Common.Domain` |
| `Marketjoya.Modules.X.Application` | su Domain, `Common.Application`, `BuildingBlocks.Contracts` |
| `Marketjoya.Modules.X.Infrastructure` | su Application, `Common.Infrastructure`, `BuildingBlocks.Contracts` |
| `Marketjoya.Modules.X.Presentation` | su Application, `Common.Presentation` |
| `Common.Application` | `Common.Domain`, `BuildingBlocks.Contracts` |
| `Common.Infrastructure` | `Common.Application`, `BuildingBlocks.Contracts` |
| `Common.Presentation` | `Common.Application` |
| `BuildingBlocks.Contracts` | nada (su ensamblado solo referencia el BCL) |
| `Marketjoya.Api` (host) | solo proyectos Infrastructure y Presentation (Common o módulos) |

Un módulo nunca referencia otro módulo (ni por proyecto ni por tipo).

## Paquetes

- Domain y Contracts: ningún paquete.
- Application: sin EF Core, Npgsql, Wolverine, RabbitMQ, MongoDB, Redis, ASP.NET Core (paquete ni framework reference).
- Presentation: sin paquetes de datos o mensajería ni Dapper.
- MediatR prohibido en todo proyecto.

## Tipos

1. Domain depende solo del BCL, `Common.Domain` y su propio módulo.
2. Application no depende de EF Core, Npgsql, Wolverine, RabbitMQ, Mongo, Redis, ASP.NET Core, ni de Infrastructure/Presentation.
3. Infrastructure no depende de Presentation: el host registra el ensamblado Presentation; `IModule` no mapea endpoints.
4. Presentation no usa frameworks de datos, `System.Data`, Dapper, Infrastructure ni puertos de datos (repositorios, read connections, `ISqlConnectionFactory`, `ITransactionManager`).
5. Handlers no dependen de EF Core (`DbContext`), `IQueryable`, Npgsql ni Infrastructure. Reciben puertos de `Abstractions/` o Domain.
6. Query handlers no usan repositorios, EF Core ni `ITransactionManager`.
7. Eventos de integración solo en `BuildingBlocks.Contracts`; Wolverine se configura y consume en Infrastructure.
8. Lecturas SQL no cruzan schemas de otros módulos (solo revisión: no es visible en código compilado).

## Estructura de los tests

```text
tests/Marketjoya.ArchitectureTests/
├── Solution/       descubrimiento de src/**/*.csproj y esquema de nombres
├── Dependencies/   LayerDependencyTests, ProjectReferenceTests
├── Conventions/    Application, Composition, Messaging, Persistence, Structure
├── Rules/          implementación de cada regla
├── Fixtures/       violaciones deliberadas (módulos Billing y Shipping)
└── SelfCheck/      cada regla debe detectar su violación
```

Un módulo nuevo queda cubierto sin editar los tests (se descubre por nombre de proyecto).

## Revisión de imports

Busca especialmente:

- `DbContext` o `IQueryable` en Application/Presentation.
- `SaveChanges` en un handler.
- `using` de EF/ASP.NET/Wolverine en Domain.
- Project reference entre módulos.
- SQL que menciona un schema ajeno en una query.
