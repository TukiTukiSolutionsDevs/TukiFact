# Plantilla: test de agregado (Domain)

Ruta: `tests/Marketjoya.Modules.Inventory.UnitTests/StockItems/StockItemTests.cs`

```csharp
namespace Marketjoya.Modules.Inventory.UnitTests.StockItems;

public sealed class StockItemTests
{
    [Fact]
    public void Reserve_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var item = new StockItemBuilder().WithQuantity(1).Build();

        // Act
        var result = item.Reserve(quantity: 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InventoryErrors.InsufficientStock(item.ProductId).Code);
        result.Error.Type.Should().Be(ErrorType.Conflict);
        item.GetDomainEvents().Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_NonPositiveQuantity_ReturnsFailure(int quantity)
    {
        // Arrange
        var item = new StockItemBuilder().WithQuantity(10).Build();

        // Act
        var result = item.Reserve(quantity);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
```

Sin IO. Builder obligatorio. Ajusta nombres de método/`Error` al agregado real; no inventes un puerto de persistencia aquí.
