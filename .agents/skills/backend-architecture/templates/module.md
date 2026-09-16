# Plantilla: módulo nuevo

Ejemplo ilustrativo con `Users`. Base real: el módulo de prueba `Shipping` en `MarketjoyaBackend/tests/Marketjoya.Common.IntegrationTests/Persistence/Fakes/`.

## Proyectos

```text
src/Modules/<Modulo>/Marketjoya.Modules.<Modulo>.Domain/
src/Modules/<Modulo>/Marketjoya.Modules.<Modulo>.Application/
src/Modules/<Modulo>/Marketjoya.Modules.<Modulo>.Infrastructure/
src/Modules/<Modulo>/Marketjoya.Modules.<Modulo>.Presentation/
tests/Marketjoya.Modules.<Modulo>.UnitTests/
tests/Marketjoya.Modules.<Modulo>.IntegrationTests/
```

Todos en `MarketjoyaBackend.sln`. Referencias: `references/dependency-rules.md`.

## Árbol mínimo

```text
Modules/<Modulo>/
├── Domain/
├── Application/
│   └── Abstractions/
│       └── I<Modulo>ReadDbConnection.cs
├── Infrastructure/
│   ├── <Modulo>Module.cs              ← raíz del proyecto
│   ├── Persistence/
│   │   ├── <Modulo>DbContext.cs
│   │   ├── Configurations/
│   │   └── Migrations/                ← generadas por dotnet-ef (generated_code en .editorconfig)
│   └── Messaging/
└── Presentation/
```

## UsersModule.cs

```csharp
namespace Marketjoya.Modules.Users.Infrastructure;

public sealed class UsersModule : IModule
{
    public string Name => UsersDbContext.SchemaName;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCommonApplication(typeof(CreateUserCommand).Assembly);
        services.AddModuleDbContext<UsersDbContext>(UsersDbContext.SchemaName);
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddReadDbConnection<IUsersReadDbConnection, UsersReadDbConnection>();
    }
}
```

- `IModule` (`Common.Infrastructure/Modules`): `Name` (minúsculas, también es el schema) y `Register`. No mapea endpoints: Infrastructure no ve Presentation.
- Exactamente un `<Modulo>Module` por módulo, en la raíz de Infrastructure (architecture test).
- Los consumers se descubren en el ensamblado de este tipo.

## UsersDbContext.cs

```csharp
namespace Marketjoya.Modules.Users.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : BaseDbContext(options)
{
    public const string SchemaName = "users";

    internal DbSet<User> Users => Set<User>();

    protected override string Schema => SchemaName;
}
```

`BaseDbContext` aplica `HasDefaultSchema`, las configuraciones del ensamblado, auditoría y `sync_version`. Los interceptores y la conexión compartida los pone `AddModuleDbContext`.

## Registro en el host

```csharp
// src/Api/Marketjoya.Api/Hosting/ModuleCatalog.cs
public static IReadOnlyCollection<IModule> Modules { get; } = [new UsersModule()];

public static IReadOnlyCollection<Assembly> EndpointAssemblies { get; } = [typeof(CreateUserEndpoint).Assembly];
```

El tag OpenAPI y el prefijo del operationId salen del nombre del ensamblado `Marketjoya.Modules.<Modulo>.Presentation`.

## Pasos

1. Crear los 4 proyectos (y los 2 de tests) con los nombres exactos, agregarlos a la solución y referenciar Common.
2. Crear `<Modulo>Module` y `<Modulo>DbContext`.
3. Registrar módulo y ensamblado Presentation en `ModuleCatalog`.
4. Primera migración (`references/common-shared-kernel.md`, Migraciones).
5. Correr `Marketjoya.ArchitectureTests`: el módulo se descubre solo.
6. Recién ahí: primer caso de uso con `checklists/new-use-case.md`.

Pendiente de decisión: si cada módulo real necesita `IDesignTimeDbContextFactory` para dotnet-ef (el módulo de prueba usa una con `ModuleDbContextOptions.Configure(options, connectionString, schema)`).

Checklist: `checklists/new-module.md`.
