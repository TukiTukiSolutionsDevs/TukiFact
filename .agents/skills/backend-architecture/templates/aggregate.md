# Plantilla: agregado (Domain)

El puerto de escritura del agregado vive en Domain si pertenece al lenguaje del dominio; si no, en `Application/Abstractions/`.

## Estructura

```text
src/Modules/Users/Marketjoya.Modules.Users.Domain/
└── Users/
    ├── User.cs
    ├── Email.cs
    ├── UserErrors.cs
    ├── Events/
    │   └── UserCreatedDomainEvent.cs
    ├── Rules/
    │   └── EmailMustBeUniqueRule.cs
    └── IUserRepository.cs
```

## Reglas

- Todo agregado hereda `AggregateRoot<TId>`; igualdad por tipo + id (`Entity<TId>`).
- Value objects heredan `ValueObject` e implementan `GetEqualityComponents()`.
- Invariantes con `IBusinessRule` y `CheckRule(rule)` (heredado de `Entity<TId>`); una regla rota lanza `BusinessRuleValidationException` → 409 `BusinessRule.<TipoDeRegla>`.
- Factory method estático (`User.Create(...)`) cubre la mayoría de creaciones. Clase Factory aparte solo con dependencias externas.
- Eventos de dominio se emiten en el agregado con `RaiseDomainEvent(...)`, no en el handler.
- Eventos: `public sealed record UserCreatedDomainEvent(DateTimeOffset OccurredOn, Guid UserId) : DomainEvent(OccurredOn);` con `OccurredOn` del reloj que recibe el agregado; nombre terminado en `DomainEvent`.
- Domain no referencia paquetes: solo BCL y `Common.Domain`.
- No declarar propiedades `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` ni `SyncVersion`: son shadow properties de `BaseDbContext`.

## IUserRepository.cs

Ver `templates/repository.md`. Solo escritura; lecturas van al query side.

## UserErrors.cs

```csharp
namespace Marketjoya.Modules.Users.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) =>
        Error.NotFound("User.NotFound", $"El usuario '{userId}' no existe.");

    public static Error EmailAlreadyInUse(string email) =>
        Error.Conflict("User.EmailAlreadyInUse", "El email ya está registrado.");
}
```

- Un `<Agregado>Errors` estático por agregado; cada miembro devuelve `Error` (`Code` + `Description` + `Type`). Handlers usan `UserErrors.NotFound(id)`; nunca `Error` inline.
- `Code` = `<Ámbito>.<Motivo>` en PascalCase (`User.NotFound`). Es API pública: no se renombra ni se reutiliza con otro significado; deprecar = dejar de emitir.
- `Description` para humanos, sin PII innecesaria; ningún cliente decide lógica con ella.
- No reutilizar códigos reservados (`<UseCase>.Validation`, `BusinessRule.*`, `Concurrency.Conflict`).

Detalle en `references/errors-and-results.md`; contrato en `references/api-error-contract.md`.
