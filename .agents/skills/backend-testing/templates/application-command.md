# Plantilla: test de command (Application)

Ejemplo ilustrativo con un módulo Users. Base real: `TransactionManagerTests` en `MarketjoyaBackend/tests/Marketjoya.Common.IntegrationTests/Persistence/`.

## Fixture y colección (una vez por módulo)

Ruta: `tests/Marketjoya.Modules.Users.IntegrationTests/UsersIntegrationFixture.cs`

```csharp
namespace Marketjoya.Modules.Users.IntegrationTests;

public sealed class UsersIntegrationFixture : ModuleIntegrationFixture
{
    private const string ReadUserSql = "SELECT id AS Id, email AS Email FROM users.users WHERE id = @UserId";

    // Rol de catálogo; cómo se siembra por colección está pendiente de decisión.
    public static readonly Guid CashierRoleId = new("00000000-0000-0000-0000-000000000001");

    protected override IModule Module { get; } = new UsersModule();

    public async Task<UserRow?> ReadUserAsync(Guid userId)
    {
        await using var scope = Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IUsersReadDbConnection>();

        return await database.Connection.QuerySingleOrDefaultAsync<UserRow>(ReadUserSql, new { UserId = userId });
    }
}

[CollectionDefinition(Name)]
public sealed class UsersCollection : ICollectionFixture<UsersIntegrationFixture>
{
    public const string Name = "Users";
}
```

## Test

Ruta: `tests/Marketjoya.Modules.Users.IntegrationTests/Users/CreateUser/CreateUserCommandHandlerTests.cs`

```csharp
namespace Marketjoya.Modules.Users.IntegrationTests.Users.CreateUser;

[Collection(UsersCollection.Name)]
public sealed class CreateUserCommandHandlerTests(UsersIntegrationFixture fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_PersistsUser()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: $"cajero-{Guid.NewGuid():N}@market.test",
            FullName: "Cajero Uno",
            Password: "password1",
            RoleId: UsersIntegrationFixture.CashierRoleId);

        // Act
        var result = await fixture.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var row = await fixture.ReadUserAsync(result.Value);
        row.Should().NotBeNull();
        row!.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflictAndDoesNotInsert()
    {
        // Arrange
        var email = $"cajero-{Guid.NewGuid():N}@market.test";
        await fixture.SendAsync(new CreateUserCommand(email, "Uno", "password1", UsersIntegrationFixture.CashierRoleId));

        // Act
        var result = await fixture.SendAsync(new CreateUserCommand(email, "Otro", "password1", UsersIntegrationFixture.CashierRoleId));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.EmailAlreadyInUse(email).Code);
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsValidationWithFieldErrors()
    {
        // Arrange
        var command = new CreateUserCommand("no-es-email", "Uno", "password1", UsersIntegrationFixture.CashierRoleId);

        // Act
        var result = await fixture.SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CreateUser.Validation");
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Description.Should().Be("La solicitud tiene errores de validación.");
        result.Error.FieldErrors.Should().ContainSingle(error => error.Field == "email" && error.Code == "Email.Invalid");
    }
}
```

Asserta `Error.Code` y `Error.Type`, nunca `Description`; en validación, `FieldErrors` con `Field` y `Code` (referencia: `tests/Marketjoya.Common.UnitTests/Application/Behaviors/ValidationBehaviorTests.cs`). `SendAsync` abre un scope por envío, como una request HTTP. No mockees `IUserRepository`. No limpies la DB: aísla con email/`Guid` únicos. El rol sembrado (`CashierRoleId`) depende del seed de catálogos, pendiente de decisión (`references/application.md`).
