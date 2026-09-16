# Plantilla: test E2E de endpoint (Presentation)

Ruta: `tests/Marketjoya.Api.E2ETests/Sales/ConfirmSaleTests.cs`

Base real: `AuthenticationTests` y `ResultMappingTests` en `MarketjoyaBackend/tests/Marketjoya.Api.E2ETests/`.

```csharp
namespace Marketjoya.Api.E2ETests.Sales;

[Collection(ApiCollection.Name)]
public sealed class ConfirmSaleTests(ApiFixture fixture)
{
    private const string Route = ApiRoutes.V1 + "/sales";

    [Fact]
    public async Task Post_ValidSale_ReturnsSuccessAndPersists()
    {
        // Arrange
        var client = fixture.CreateAuthenticatedClient(TestCaller.Unique(), "sales.create", "sales.read");
        var body = new ConfirmSaleRequest(/* ids únicos del test */);

        // Act
        var response = await client.PostAsJsonAsync(Route, body, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        var get = await client.GetAsync($"{Route}/{id}", TestContext.Current.CancellationToken);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_WithoutPermission_Returns403Problem()
    {
        // Arrange
        var client = fixture.CreateAuthenticatedClient(TestCaller.Unique());

        // Act
        var response = await client.PostAsJsonAsync(Route, new ConfirmSaleRequest(/* ... */), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_InsufficientStock_Returns409ProblemWithCode()
    {
        // Arrange
        var client = fixture.CreateAuthenticatedClient(TestCaller.Unique(), "sales.create");

        // Act
        var response = await client.PostAsJsonAsync(Route, new ConfirmSaleRequest(/* sin stock */), TestContext.Current.CancellationToken);

        // Assert
        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.Conflict);
        problem.Extension("code").Should().Be(InventoryErrors.InsufficientStock(/* producto */).Code);
    }

    [Fact]
    public async Task Post_InvalidQuantity_Returns400ProblemWithFieldErrors()
    {
        // Arrange
        var client = fixture.CreateAuthenticatedClient(TestCaller.Unique(), "sales.create");

        // Act
        var response = await client.PostAsJsonAsync(Route, new ConfirmSaleRequest(/* items[0].quantity = 0 */), TestContext.Current.CancellationToken);

        // Assert
        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
        problem.Extension("code").Should().Be("ConfirmSale.Validation");
        problem.Detail.Should().Be("La solicitud tiene errores de validación.");
        var errors = (JsonElement)problem.Extensions["errors"]!;
        errors.EnumerateArray().Should().Contain(error =>
            error.GetProperty("field").GetString() == "items[0].quantity"
            && error.GetProperty("code").GetString() == "Quantity.GreaterThanZero");
    }
}
```

Asserts de error según `../../backend-architecture/references/api-error-contract.md` §7: status, `code`, `traceId` y `correlationId` (los dos últimos los valida `ShouldBeProblemAsync`); 400 incluye el `detail` fijo y `errors` con `field` y `code` (el de `WithErrorCode`; `Validation.<Regla>` solo si la regla no lo declara, y eso lo prueba Common). No asertar otros `detail` ni `description`. Referencia del 400 con `errors` en el host: `tests/Marketjoya.Api.E2ETests/Errors/ValidationProblemTests.cs`.

`ApiCollection` comparte `ApiFixture` (Postgres, Redis, RabbitMQ, Mongo) y un host real (`ApiFactory`). Migraciones y contenedores una vez. `TestCaller.Unique()` da claims de usuario, empresa, almacén y caja únicos. Cada venta usa ids únicos. Éxito con `Result<T>` → 200 (201 + `Location` pendiente de decisión). Sustitución de SUNAT pendiente de decisión (`references/presentation.md`). Reserva E2E para flujos críticos.
