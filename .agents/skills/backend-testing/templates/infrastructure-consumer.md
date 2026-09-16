# Plantilla: test de consumer (Infrastructure)

Ruta: `tests/Marketjoya.Modules.Inventory.IntegrationTests/Messaging/SaleCompletedConsumerTests.cs`

Base real: `IntegrationEventConsumerTests` y `MessagingFixture` en `MarketjoyaBackend/tests/Marketjoya.Common.IntegrationTests/Messaging/`. El fixture de mensajería por módulo está pendiente de decisión (`references/infrastructure.md`); los nombres `InventoryMessagingCollection`/`InventoryMessagingFixture` son ilustrativos.

```csharp
namespace Marketjoya.Modules.Inventory.IntegrationTests.Messaging;

[Collection(InventoryMessagingCollection.Name)]
public sealed class SaleCompletedConsumerTests(InventoryMessagingFixture fixture)
{
    [Fact]
    public async Task Consume_SameMessageDeliveredTwice_AppliesOnce()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var integrationEvent = new SaleCompletedIntegrationEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow, saleId);

        // Act
        await fixture.PublishToBrokerAsync(integrationEvent, deliveries: 2);
        await Eventually.SatisfiesAsync(
            () => fixture.CountEffectsAsync(saleId),
            effects => effects >= 1,
            "the first delivery is handled");

        // Assert
        (await fixture.CountEffectsAsync(saleId)).Should().Be(1);
    }
}
```

Fixture: Postgres + RabbitMQ + Mongo (`ContainerImages`) y host con `AddCommonMessaging` (consumers del ensamblado del módulo). `PublishToBrokerAsync` publica el mismo id de mensaje al exchange `marketjoya.<TipoDeEvento>`. `Eventually.SatisfiesAsync` hace polling con timeout. El consumer no contiene reglas de stock: llama al caso de uso; este test demuestra inbox, no el agregado.

Dead letter: con retries acortados en `MessagingOptions`, aserta el documento en Mongo `dead_letters` (`correlation_id`, `message_type`, `exception_type`, `payload`).
