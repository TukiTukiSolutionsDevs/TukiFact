# Plantilla: comando (escritura)

Ejemplo ilustrativo: `CreateUser` en un módulo Users (aún no existe en código).

## Estructura

```text
Application/Users/CreateUser/
├── CreateUserCommand.cs
├── CreateUserCommandHandler.cs
└── CreateUserCommandValidator.cs

Presentation/Users/CreateUser/
├── CreateUserEndpoint.cs
└── CreateUserRequest.cs
```

Todo lo que solo este caso de uso necesita vive en su carpeta. Si un DTO/mapper se repite, se promueve a `Users/Shared/`.

## CreateUserCommand.cs

```csharp
namespace Marketjoya.Modules.Users.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string FullName,
    string Password,
    Guid RoleId) : IRequest<Result<Guid>>;
```

- `public sealed record`, posicional.
- `IRequest<Result<Guid>>` (id del creado) o `IRequest` (= `IRequest<Result>`) si no hay dato de salida.
- El sufijo `Command` activa `TransactionBehavior`; sin ese sufijo, implementar `ICommand`.
- Sin anotaciones de validación: viven en el validator.

## CreateUserCommandValidator.cs

```csharp
namespace Marketjoya.Modules.Users.Application.Users.CreateUser;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("Email.Required")
            .EmailAddress().WithErrorCode("Email.Invalid")
            .MaximumLength(256).WithErrorCode("Email.TooLong");
        RuleFor(x => x.FullName)
            .NotEmpty().WithErrorCode("FullName.Required")
            .MaximumLength(200).WithErrorCode("FullName.TooLong");
        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("Password.Required")
            .MinimumLength(8).WithErrorCode("Password.TooShort");
        RuleFor(x => x.RoleId).NotEmpty().WithErrorCode("RoleId.Required");
    }
}
```

- Solo formato y presencia. Reglas de negocio ("email único") van en el dominio (`IBusinessRule`).
- Cada regla declara `WithErrorCode("<Campo>.<Regla>")`; el código es API pública y no se renombra. Sin él, el backend emite `Validation.<Regla>` (`Validation.NotEmpty`): no dejarlo a propósito.
- Un fallo devuelve `Error.Validation("CreateUser.Validation", "La solicitud tiene errores de validación.")` con un `FieldError` por regla rota (`field` = `email`, `code` = `Email.Invalid`) sin abrir transacción → 400 con ese `detail` fijo y `errors`. Detalle: `references/errors-and-results.md` (Validación).
- Las propiedades del Command llevan los mismos nombres que las del Request para que `field` apunte al cuerpo HTTP.

## CreateUserCommandHandler.cs

```csharp
namespace Marketjoya.Modules.Users.Application.Users.CreateUser;

internal sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IClock clock)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        var userResult = User.Create(
            emailResult.Value,
            command.FullName,
            passwordHasher.Hash(command.Password),
            command.RoleId,
            clock.UtcNow,
            new EmailMustBeUniqueChecker(userRepository));

        if (userResult.IsFailure)
        {
            return Result.Failure<Guid>(userResult.Error);
        }

        userRepository.Add(userResult.Value);

        return Result.Success(userResult.Value.Id);
    }
}
```

Reglas del handler (architecture tests):

- `internal sealed`, primary constructor con puertos, mismo namespace y carpeta que el command.
- Orquesta; no contiene reglas de negocio.
- Nunca llama `SaveChanges` ni abre transacciones (`TransactionBehavior` lo hace).
- Nunca recibe `DbContext`, `IQueryable`, tipos de Npgsql ni de Infrastructure.

El request y el endpoint están en `templates/endpoint.md`.

## Flujo

```text
HTTP POST /api/v1/users
  → CreateUserEndpoint → CreateUserRequest.ToCommand() → ISender.Send
      → LoggingBehavior → TelemetryBehavior → ValidationBehavior
      → TransactionBehavior (ITransactionManager.BeginTransactionAsync)
          → CreateUserCommandHandler → User.Create(...) + UserCreatedDomainEvent → userRepository.Add(user)
      → CommitAsync: SaveChangesAsync de cada DbContext enlistado
          (IDomainEventHandler<T>, auditoría, sync_version, outbox) → commit → flush del outbox
  ← Result<Guid> → ToHttpResult() → 200 con el id
```

## Variantes

| Caso | Diferencia |
|---|---|
| Command sin salida | `IRequest`; `ToHttpResult()` devuelve 204 |
| Id en ruta | Endpoint toma `Guid id` y lo pasa: `request.ToCommand(id)` |
| Lock pesimista (stock) | Puerto `GetByIdForUpdateAsync` (`SELECT ... FOR UPDATE`); solo con contención real |
| Evento de integración | `IDomainEventHandler<T>` traduce y publica con `IIntegrationEventPublisher` (outbox, misma transacción) |
| Autorización de supervisor | Command lleva `AuthorizationTicket`; el agregado verifica la regla |
| Creación con 201 + `Location` | Pendiente de decisión (`ToHttpResult()` no lo produce) |
