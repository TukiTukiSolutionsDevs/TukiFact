# Plantilla: test data builder

Ruta: `tests/Marketjoya.Modules.Sales.UnitTests/Sales/Builders/SaleBuilder.cs`

Los builders son obligatorios cuando el agregado tiene invariantes. IntegrationTests reutiliza este proyecto para no armar objetos a mano. Bogus aún no está en `Directory.Packages.props`: se agrega con el primer builder que lo use.

```csharp
namespace Marketjoya.Modules.Sales.UnitTests.Sales.Builders;

public sealed class SaleBuilder
{
    private Guid _registerId = Guid.NewGuid();

    public SaleBuilder ForRegister(Guid registerId)
    {
        _registerId = registerId;
        return this;
    }

    public SaleBuilder WithItem(/* ítem válido del lenguaje de Sales */) => this;

    public SaleBuilder PaidWith(/* medio de pago */) => this;

    public Sale Build() =>
        Sale.Create(_registerId, /* resto de invariantes */).Value;
}
```

Uso: `new SaleBuilder().ForRegister(x).WithItem(...).PaidWith(...).Build()`.

- Fluible; un método por invariante relevante (`PaidWithCash`, `WithQuantity`).
- Bogus para datos irrelevantes (nombres, emails); seed fija si el test es determinista.
- `Build()` debe producir un agregado válido. Los tests de fallo parten de uno válido y luego ejecutan la operación que rompe la regla.
- Prohibido un constructor de test que salte `Create` y deje el agregado anémico.
