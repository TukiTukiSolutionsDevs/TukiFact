# Plantilla: query (lectura)

Ejemplo ilustrativo: `GetUserById` y `SearchUsers` en un módulo Users (aún no existe en código).

## Estructura

```text
Application/Abstractions/
└── IUsersReadDbConnection.cs

Application/Users/GetUserById/
├── GetUserByIdQuery.cs
├── GetUserByIdQueryHandler.cs
├── GetUserByIdResponse.cs
└── GetUserByIdMapper.cs          ← solo si este caso de uso lo necesita

Presentation/Users/GetUserById/
└── GetUserByIdEndpoint.cs

Infrastructure/Users/
└── UsersReadDbConnection.cs
```

Las queries no llevan validator salvo filtros de entrada; en ese caso `GetUserByIdQueryValidator` (`internal sealed`) en la misma carpeta.

## Puerto de lectura

```csharp
namespace Marketjoya.Modules.Users.Application.Abstractions;

public interface IUsersReadDbConnection
{
    DbConnection Connection { get; }
}
```

```csharp
namespace Marketjoya.Modules.Users.Infrastructure.Users;

internal sealed class UsersReadDbConnection(ISqlConnectionFactory connectionFactory)
    : ReadDbConnection(connectionFactory), IUsersReadDbConnection;
```

Registro en `UsersModule.Register`: `services.AddReadDbConnection<IUsersReadDbConnection, UsersReadDbConnection>();`.

## GetUserByIdQuery.cs

```csharp
namespace Marketjoya.Modules.Users.Application.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<GetUserByIdResponse>>;
```

## GetUserByIdResponse.cs

```csharp
namespace Marketjoya.Modules.Users.Application.Users.GetUserById;

public sealed record GetUserByIdResponse(
    Guid Id,
    string Email,
    string FullName,
    string RoleName,
    bool IsActive,
    DateTimeOffset CreatedAt);
```

`record` plano, sin tipos de dominio. Prohibido exponer entidades o agregados. `DateOnly` y `DateTimeOffset` (`timestamptz`) se mapean con los type handlers de Common.

## GetUserByIdQueryHandler.cs

```csharp
namespace Marketjoya.Modules.Users.Application.Users.GetUserById;

internal sealed class GetUserByIdQueryHandler(IUsersReadDbConnection db)
    : IRequestHandler<GetUserByIdQuery, Result<GetUserByIdResponse>>
{
    private const string Sql = """
        SELECT u.id          AS Id,
               u.email       AS Email,
               u.full_name   AS FullName,
               r.name        AS RoleName,
               u.is_active   AS IsActive,
               u.created_at  AS CreatedAt
        FROM users.users u
        JOIN users.roles r ON r.id = u.role_id
        WHERE u.id = @UserId
        """;

    public async Task<Result<GetUserByIdResponse>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var response = await db.Connection.QuerySingleOrDefaultAsync<GetUserByIdResponse>(
            new CommandDefinition(Sql, new { query.UserId }, cancellationToken: cancellationToken));

        return response is null
            ? Result.Failure<GetUserByIdResponse>(UserErrors.NotFound(query.UserId))
            : Result.Success(response);
    }
}
```

- SQL con CTEs/joins lo complejo que haga falta, dentro del schema propio: el query side no pasa por el dominio.
- Alias de columna → Response (Dapper); columnas en snake_case.
- Error estático en Domain (`UserErrors.NotFound`); no se construye inline.

El endpoint está en `templates/endpoint.md`.

## Reglas del query side

1. Dapper + SQL directo vía `I<Modulo>ReadDbConnection` (puerto en `Application/Abstractions/`, implementación sobre `ReadDbConnection` en Infrastructure).
2. Nunca EF Core, agregados, repositorios, `IQueryable` ni `ITransactionManager` (architecture test).
3. Sin tracking y sin transacción: `TransactionBehavior` no aplica a `*Query`.
4. Lecturas a otro schema = prohibidas. Dato de otro módulo: contrato público o proyección local por eventos de integración.
5. Paginación con `PagedResult<T>(Items, TotalCount, PageNumber, PageSize)` de `Common.Domain`; `LIMIT/OFFSET` o keyset en tablas grandes.

## Shared

Primer uso: DTO/mapper en la carpeta del caso de uso. Segundo uso real: promover a `Users/Shared/` y actualizar el primero.

```text
Application/Users/
├── GetUserById/ ... (usa Shared/UserDto)
├── SearchUsers/
│   ├── SearchUsersQuery.cs
│   ├── SearchUsersQueryHandler.cs
│   └── SearchUsersResponse.cs    ← List<UserDto> dentro
└── Shared/
    ├── UserDto.cs
    └── UserMapper.cs
```

- Prohibido crear `Shared/` vacío o por si acaso.
- `Shared/` es por submódulo, nunca a nivel de capa (architecture test).
- Si los campos empiezan a divergir, se separa de nuevo.
