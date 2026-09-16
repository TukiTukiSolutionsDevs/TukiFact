# Convenciones y anti-patrones

## Nomenclatura

| Elemento | Patrón | Ejemplo |
|---|---|---|
| Proyecto | `Marketjoya.Modules.<Modulo>.<Capa>` | `Marketjoya.Modules.Users.Application` |
| Command | `Verbo` + `Sustantivo` + `Command` (o implementa `ICommand`) | `CreateUserCommand` |
| Query | `Get/Search` + `Sustantivo` + `Query` | `GetUserByIdQuery` |
| Handler | request + `Handler`, junto al request | `CreateUserCommandHandler` |
| Validator | mensaje + `Validator`, mismo namespace | `CreateUserCommandValidator` |
| Request (API) | `Verbo` + `Sustantivo` + `Request` | `CreateUserRequest` |
| Response | caso de uso + `Response` | `GetUserByIdResponse` |
| Endpoint | caso de uso + `Endpoint` | `GetUserByIdEndpoint` |
| Módulo | módulo + `Module`, raíz de Infrastructure | `UsersModule` |
| Repositorio (puerto) | `I` + agregado + `Repository` | `ISaleRepository` |
| Read connection (puerto) | `I` + módulo + `ReadDbConnection` | `IUsersReadDbConnection` |
| DbContext | módulo + `DbContext`, en `Infrastructure/Persistence` | `UsersDbContext` |
| Evento de dominio | sustantivo + verbo pasado + `DomainEvent`, en Domain | `SaleCompletedDomainEvent` |
| Evento de integración | sustantivo + verbo pasado + `IntegrationEvent`, en Contracts | `SaleCompletedIntegrationEvent` |
| Consumer | evento + `Consumer`, en `Infrastructure/Messaging` | `SaleCompletedConsumer` |
| Error | agregado + `Errors`, estático en Domain | `UserErrors.NotFound(id)` |
| Código de error | `<Ámbito>.<Motivo>` PascalCase, estable (no se renombra) | `Sale.NotFound` |
| Schema Postgres | minúsculas, = `IModule.Name` | `sales`, `inventory`, `identity` |

- Un tipo por archivo; archivo = tipo. Namespace file-scoped = ruta de carpetas (`.editorconfig` lo marca como warning).
- Handlers y validators: `internal sealed`. Commands/Queries: `public sealed record`. Endpoints: `sealed`. Consumers: `public sealed`.
- `record` posicional para mensajes y DTOs; clases para agregados, handlers, endpoints y repositorios.

## Repositorio

Prohibido el repositorio genérico y los repositorios inflados. Plantilla: `templates/repository.md`.

1. Nunca `IRepository<T>` ni `BaseRepository<T>` (ningún tipo genérico terminado en `Repository`).
2. Un repositorio por agregado, solo escritura.
3. Máximo 5 métodos públicos. Lecturas → query side (Dapper).
4. Invariantes que cruzan datos (deuda, tope de crédito) → domain service, no un método más del repo.
5. Ningún método devuelve `IQueryable`; `DbContext` no sale de Infrastructure.
6. Prohibidos `IUnitOfWork` y `UnitOfWork`: confirma `TransactionBehavior` vía `ITransactionManager`.

Las reglas 1, 3, 5 y 6 las verifica `PersistenceConventionTests`.

## Anti-patrones vetados

- Carpetas horizontales `Handlers/`, `Services/`, `Managers/`, `Helpers/`, `Dtos/`, `Repositories/` en cualquier capa.
- `Shared/` a nivel de capa (solo dentro de un submódulo y con uso real).
- Lógica de negocio en endpoints, validators o consumers.
- `SaveChanges` desde un handler.
- AutoMapper global; mappers pequeños junto al caso de uso o en `Shared/` del submódulo.
- Referencias entre módulos (solo `BuildingBlocks.Contracts`).
- Joins SQL a schemas de otros módulos.
- Anemia: agregados con setters públicos y reglas en el handler.
- DTOs de dominio (exponer entidades en Responses).
- Propiedades de dominio con nombres reservados de auditoría (`CreatedAt`, `SyncVersion`, ...).
- Specification como capa de consulta sobre repositorio (`repo.Find(spec)`). Las specifications evalúan dominio en memoria; las consultas van al query side. Ver `references/design-patterns.md`.
- Patrones "por si acaso": 2–3 variaciones reales, no antes.
- Singleton clásico con `Instance` estático; el lifetime lo gestiona DI.
- Decoradores manuales para lógica que un behavior del mediador ya resuelve.

Eventos: `references/events.md`. Errores: `references/errors-and-results.md`.
