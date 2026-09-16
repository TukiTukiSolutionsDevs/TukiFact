# Eventos: dominio vs integración

| | Dominio | Integración |
|---|---|---|
| Transporte | Mediador propio (`IPublisher`), in-process, misma transacción | Wolverine 6.37 → RabbitMQ, outbox durable en Postgres |
| Definido en | `Marketjoya.Modules.X.Domain`, `*DomainEvent` (record de `DomainEvent`) | `Marketjoya.BuildingBlocks.Contracts`, `*IntegrationEvent` (record de `IntegrationEvent`) |
| Emitido por | Agregado (`RaiseDomainEvent`) | Handler de dominio vía `IIntegrationEventPublisher` |
| Atendido por | `IDomainEventHandler<T>` en Application del mismo módulo | `public sealed <X>Consumer : IIntegrationEventConsumer<T>` en `Infrastructure/Messaging` del módulo interesado |
| Uso | Reacciones dentro del bounded context | Comunicación entre módulos |

## Flujo obligatorio

```text
agregado emite evento de dominio
  → SaveChangesAsync (DomainEventsInterceptor) → IDomainEventHandler<T>
  → handler traduce a evento de integración → IIntegrationEventPublisher (outbox, misma transacción)
  → commit → flush a RabbitMQ
  → consumer con inbox idempotente
```

Nunca publicar desde un handler de comando sin pasar por el evento de dominio.

## Contratos (`Marketjoya.BuildingBlocks.Contracts`)

- Solo BCL. `IIntegrationEvent`: `Id` (único por hecho; los consumers deduplican por él), `OccurredOn`, `CorrelationId`, `Version`.
- `abstract record IntegrationEvent(Guid Id, DateTimeOffset OccurredOn)`: `CorrelationId { get; init; }`, `Version` = 1 por defecto.

```csharp
public sealed record SaleCompletedIntegrationEvent(Guid Id, DateTimeOffset OccurredOn, Guid SaleId)
    : IntegrationEvent(Id, OccurredOn);
```

## Publicación

- `IIntegrationEventPublisher.PublishAsync(evento)` escribe en el outbox de Wolverine con la conexión y la transacción del comando.
- Tras el commit se hace flush a RabbitMQ; si el flush falla se registra el error y los envelopes quedan en el outbox para recuperación. Tras rollback se descartan.
- Fuera de una transacción de comando lanza `InvalidOperationException`.
- Si el evento no trae `CorrelationId`, el publisher lo completa desde `ICorrelationIdAccessor`. El id del envelope = `Id` del evento.

## Consumo

- Solo se descubren tipos que implementan `IIntegrationEventConsumer<T>` en los ensamblados de cada `IModule` (el host los agrega); el descubrimiento convencional de handlers de Wolverine está deshabilitado.
- Consumer traduce y llama un caso de uso o puerto; sin reglas de negocio. Se resuelve desde el scope DI por mensaje.
- El middleware de Wolverine restaura el correlation id del envelope en `ICorrelationIdAccessor` y lo etiqueta como `marketjoya.correlation_id`.
- Inbox durable: deduplicación por (id de mensaje, cola).
- Siempre pasa por RabbitMQ, aunque el consumer viva en el mismo proceso (routing local deshabilitado).

## Nombres (Wolverine)

| Elemento | Valor |
|---|---|
| Schema de almacenamiento | `wolverine` |
| Exchange | `marketjoya.<TipoDeEvento>` |
| Cola | `marketjoya.<Modulo>.<TipoDeConsumer>` (módulo = segmento tras `Modules` en el namespace) |

## Fallos

Cualquier excepción del consumer:

1. Reintentos inmediatos con espera: 50 ms, 500 ms, 2 s.
2. Reintentos programados: 30 s, 5 min, 30 min.
3. Cola de error: dead letters de Wolverine en Postgres (reproducibles) + archivo en Mongo `dead_letters` (`correlation_id`, `message_type`, `exception_type`, `payload`, ...). Si el archivo en Mongo falla, solo se registra.

Los tiempos son `MessagingOptions` (configurables en `AddCommonMessaging`).

Configuración: `ConnectionStrings:Database`, `ConnectionStrings:RabbitMq`, `ConnectionStrings:Mongo`, `Mongo:AuditDatabase`. Trazabilidad: `references/telemetry.md`.
