# Plantilla: endpoint (Presentation)

Un endpoint por caso de uso, carpeta espejo. Solo recibe request → mapea a Command/Query → `ISender.Send` → `ToHttpResult()`. Ejemplo ilustrativo con un módulo Users; referencia real: los endpoints de prueba en `MarketjoyaBackend/tests/Marketjoya.Api.E2ETests/Fakes/Probe/`.

## Qué pone el host (no repetir)

`MapEndpoints()` (`Common.Presentation/Endpoints`) mapea cada `IEndpoint` dentro de:

- el grupo versionado `/api/v{version:apiVersion}` con `ApiVersions.V1` (Asp.Versioning 10.2.3, segmento de URL);
- un grupo por módulo con tag = nombre del módulo, tomado del ensamblado `Marketjoya.Modules.<Modulo>.Presentation`;
- `RequireAuthorization()`: autenticado por defecto (además la fallback policy exige autenticación);
- respuestas 401 (y 403 si hay permiso o rol) documentadas automáticamente.

La ruta del endpoint es relativa: `/users` queda en `/api/v1/users`. Sin prefijo de versión, versión no soportada (`/api/v2/...`) o ruta desconocida → 404.

## Command — CreateUserRequest.cs

```csharp
namespace Marketjoya.Modules.Users.Presentation.Users.CreateUser;

public sealed record CreateUserRequest(
    string Email,
    string FullName,
    string Password,
    Guid RoleId)
{
    public CreateUserCommand ToCommand() =>
        new(Email, FullName, Password, RoleId);
}
```

El mapeo Request→Command es `ToCommand()` en el propio Request. Si crece, un `CreateUserMapper.cs` en la misma carpeta.

## Command — CreateUserEndpoint.cs

```csharp
namespace Marketjoya.Modules.Users.Presentation.Users.CreateUser;

public sealed class CreateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/users", async (CreateUserRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(), cancellationToken);
                return result.ToHttpResult();
            })
            .RequireAuthorization("users.create")
            .WithName("CreateUser")
            .WithSummary("Creates a user.")
            .Produces<Guid>()
            .WithStandardProblems();
}
```

## Query — GetUserByIdEndpoint.cs

```csharp
namespace Marketjoya.Modules.Users.Presentation.Users.GetUserById;

public sealed class GetUserByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/users/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .RequireAuthorization("users.read")
            .WithName("GetUserById")
            .WithSummary("Gets a user by id.")
            .Produces<GetUserByIdResponse>()
            .WithStandardProblems();
}
```

Si la query tiene filtros, define `SearchUsersRequest` en su carpeta (`[AsParameters]` o body) y lo mapea a la Query.

## Reglas

- `sealed <CasoDeUso>Endpoint` en `Presentation/<Submodulo>/<CasoDeUso>/` (architecture test). `XRequest` vive junto a su endpoint.
- `.WithName("<CasoDeUso>")` obligatorio y único: operationId `<Modulo>_<CasoDeUso>`. El build falla si falta.
- `.WithSummary("<una línea>")`.
- Sin `.WithTags(...)`: el tag lo pone el grupo del módulo.
- `.WithStandardProblems()` documenta 400/404/409/500 como `application/problem+json` con la forma del contrato (ver Respuestas de error).
- `.Produces<T>()` (o tipos `TypedResults`) para el cuerpo de éxito; `.Produces(204)` en commands sin cuerpo.
- `.RequireAuthorization("<permiso>")`: todo nombre de política no registrado es un permiso; exige usuario autenticado con el claim `permissions` igual a ese valor (401 sin token, 403 sin permiso). `.AllowAnonymous()` solo cuando es intencional.
- Prohibida lógica de negocio, validaciones de dominio o acceso a datos.
- Ningún endpoint construye `ProblemDetails` a mano; usa `ToHttpResult()`.
- Más de un caso de uso por carpeta está prohibido.

## Respuestas de error

Las produce `ToHttpResult()` y los exception handlers del host según `references/api-error-contract.md` §4; el endpoint no añade nada.

| Status | Origen | `code` | Extensiones |
|---|---|---|---|
| 400 | `ValidationBehavior` | `<CasoDeUso>.Validation` (`CreateUser.Validation`) | `traceId`, `correlationId`, `errors` (`field`, `code`, `description`); `detail` fijo "La solicitud tiene errores de validación." |
| 404 / 409 | `Result.Failure(<Agregado>Errors.X)`, regla de dominio, concurrencia | `Error.Code`, `BusinessRule.<Regla>`, `Concurrency.Conflict` | `traceId`, `correlationId` |
| 500 | `Error.Failure` / excepción no controlada | `Error.Code` / ausente | `traceId`, `correlationId`; `detail` sin descripción interna |
| 400 de JSON mal formado / 401 / 403 / 404 de ruta | framework | ausente (el cliente aplica el reservado de §3) | `traceId`, `correlationId` |

El `type` del cuerpo es la URI del RFC; la categoría no viaja y los clientes la derivan del status. Los `code` de un endpoint publicado no se renombran: frontend y mobile ramifican por ellos.

## Claims del JWT

HS256 firmado con `Jwt:Secret`. Claims tal como se emiten: `sub` (usuario), `company` y `warehouse` (un claim por id), `cash_register`, `permissions` (un claim por permiso). `ICurrentUser` los expone como `UserId`, `CompanyIds`, `WarehouseIds`, `CashRegisterId`.

## Contrato OpenAPI

- Generado con `Microsoft.AspNetCore.OpenApi` (OpenAPI 3.1). `dotnet build` reescribe `openapi/marketjoya-api-v1.json`; commitear el archivo en el mismo commit que el endpoint.
- Deriva detectada por CI (`git diff --exit-code -- openapi/`) y por `CommittedOpenApiDocumentTests`.
- `/openapi/v1.json` y la UI Scalar (`/scalar`) solo en Development.
- Cambios aditivos quedan en `v1`; cambios que rompen salen como `v2`.
- Pendiente de decisión: enums se serializan como números (no hay `JsonStringEnumConverter`).

Detalle: `MarketjoyaBackend/docs/openapi.md`.
