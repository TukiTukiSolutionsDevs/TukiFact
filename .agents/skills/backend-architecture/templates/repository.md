# Plantilla: repositorio de escritura

Un repositorio por agregado, solo escritura. EF Core ya es repositorio; la transacción la maneja `TransactionBehavior` vía `ITransactionManager`. Prohibido `IRepository<T>` y `BaseRepository<T>`.

## Puerto

Interfaz en Domain (si es lenguaje del dominio) o en `Application/Abstractions/`. Implementación EF en Infrastructure del submódulo.

```csharp
public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(SaleId id, CancellationToken cancellationToken);
    Task<bool> ExistsByClientMutationIdAsync(DeviceId device, Guid mutationId, CancellationToken cancellationToken);
    void Add(Sale sale);
}
```

Métodos típicos: `GetByIdAsync`, `Add`, `ExistsBy...`, `GetByIdForUpdateAsync` (lock pesimista solo con contención real, p. ej. `StockItem`).

Máximo 5 métodos públicos y ninguno devuelve `IQueryable` (`PersistenceConventionTests`). Si crece, las lecturas se mueven al query side (Dapper).

## Implementación

```csharp
namespace Marketjoya.Modules.Users.Infrastructure.Users;

internal sealed class UserRepository(UsersDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}
```

```text
Infrastructure/Users/UserRepository.cs          ← : IUserRepository (EF Core)
Infrastructure/Users/UsersReadDbConnection.cs   ← : ReadDbConnection, IUsersReadDbConnection (Dapper)
```

Registro en `<Modulo>Module.Register`: `services.AddScoped<IUserRepository, UserRepository>();`.

## Prohibido

- `GetAll()`, `Find(Expression<...>)`, métodos de reporte.
- Exponer `IQueryable` o `DbContext` fuera de Infrastructure.
- `IUnitOfWork`/`UnitOfWork`: `TransactionBehavior` + `ITransactionManager.CommitAsync` (que llama `SaveChangesAsync` de cada contexto) hacen el commit.
- Consultas de invariante (deuda, tope de crédito) en el repositorio: van a un domain service.

Detalle: `references/conventions.md`.
