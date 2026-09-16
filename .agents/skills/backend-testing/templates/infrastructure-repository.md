# Plantilla: test de repositorio (Infrastructure)

Ruta: `tests/Marketjoya.Modules.Sales.IntegrationTests/Persistence/SaleRepositoryTests.cs`

Solo si el mapping EF es el riesgo. Si `ConfirmSale` ya persiste el agregado, no dupliques un roundtrip vacío.

```csharp
namespace Marketjoya.Modules.Sales.IntegrationTests.Persistence;

[Collection(SalesCollection.Name)]
public sealed class SaleRepositoryTests(SalesIntegrationFixture fixture)
{
    [Fact]
    public async Task Add_ThenGetById_ReturnsSameAggregate()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var sale = new SaleBuilder().PaidWith(/* cash */).Build();

        // Act
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var transactions = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
            await transactions.BeginTransactionAsync(cancellationToken);
            scope.ServiceProvider.GetRequiredService<ISaleRepository>().Add(sale);
            await transactions.CommitAsync(cancellationToken);
        }

        await using var readScope = fixture.Services.CreateAsyncScope();
        var loaded = await readScope.ServiceProvider.GetRequiredService<ISaleRepository>().GetByIdAsync(sale.Id, cancellationToken);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(sale.Id);
    }
}
```

`CommitAsync` hace `SaveChangesAsync` de los DbContext del scope (interceptores incluidos) y confirma **esta** venta en la DB compartida. Leer en otro scope evita el change tracker. No recrea schema. El handler de Application sigue sin llamar `SaveChanges`.
