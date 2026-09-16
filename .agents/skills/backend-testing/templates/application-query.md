# Plantilla: test de query (Application)

Ruta: `tests/Marketjoya.Modules.Users.IntegrationTests/Users/GetUserById/GetUserByIdQueryHandlerTests.cs`

Fixture y colección: `templates/application-command.md`.

```csharp
namespace Marketjoya.Modules.Users.IntegrationTests.Users.GetUserById;

[Collection(UsersCollection.Name)]
public sealed class GetUserByIdQueryHandlerTests(UsersIntegrationFixture fixture)
{
    [Fact]
    public async Task Handle_ExistingId_ReturnsResponse()
    {
        // Arrange
        var email = $"cajero-{Guid.NewGuid():N}@market.test";
        var created = await fixture.SendAsync(new CreateUserCommand(email, "Cajero Uno", "password1", UsersIntegrationFixture.CashierRoleId));

        // Act
        var result = await fixture.SendAsync(new GetUserByIdQuery(created.Value));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(email);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await fixture.SendAsync(new GetUserByIdQuery(id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.NotFound(id).Code);
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
```

SQL real vía Dapper. Seed de **este** test (id único) por el caso de uso o builders. No resetees la DB aquí. No uses EF/`IQueryable` en el handler ni joins a schemas ajenos.
