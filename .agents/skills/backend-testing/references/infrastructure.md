# Capa: Infrastructure

Qué vive aquí: `<M>Module`, `<M>DbContext` y configurations EF, repositorios, `I<M>ReadDbConnection`, consumers de eventos de integración, clientes HTTP.

No hay reglas de negocio. Se prueba que el adapter cumple el puerto contra infra real.

## Qué probar

| Pieza | Comportamiento a demostrar |
|---|---|
| Repositorio | Roundtrip del agregado (Add + GetById); `ExistsBy...`; `GetByIdForUpdateAsync` solo si hay contención real |
| EF configuration | Tabla en el schema propio, conversiones de VO, auditoría y `sync_version` llenados por interceptor |
| Read connection | SQL de una query representativa contra el schema propio |
| Outbox | Evento de integración enviado una vez tras el commit; descartado si la transacción hace rollback |
| Consumer | Inbox idempotente: el mismo mensaje dos veces no duplica el efecto |
| Consumer | Retries y dead letter (Wolverine en Postgres + Mongo `dead_letters`) cuando el consumer falla siempre |
| Cliente HTTP | No se prueba el proveedor real: se mockea (NSubstitute) en tests de Application/E2E |

## Qué no probar

- LINQ interno de EF ni el pipeline de Wolverine como framework.
- Lógica de dominio dentro del consumer (el consumer traduce y llama un caso de uso o puerto; el caso de uso ya tiene test).
- Mappers DTO↔dominio triviales campo a campo.

## Dónde

```text
tests/Marketjoya.Modules.<M>.IntegrationTests/
├── Persistence/                         ← opcional, si el roundtrip no queda cubierto por el command
│   └── <Agregado>RepositoryTests.cs
└── Messaging/
    └── <Evento>ConsumerTests.cs
```

Si el command de Application ya demuestra persistencia + outbox, no dupliques un repository test vacío. Añade Persistence solo cuando el mapping EF es el riesgo (VO complejos, `FOR UPDATE`).

## Cómo

- Persistencia: misma colección que Application (`ModuleIntegrationFixture`). Sin limpiar ni recrear schema por test. Identidad única por agregado.
- Fuera del mediador, confirma con el puerto real: `ITransactionManager.BeginTransactionAsync` → `repo.Add` → `CommitAsync` en un scope; lee en otro scope.
- Mensajería: fixture de colección con Postgres + RabbitMQ + Mongo y host Wolverine real (`AddCommonMessaging`). Referencia: `MessagingFixture` en `Marketjoya.Common.IntegrationTests/Messaging` (`SendAsync` con correlation id, `TrackAsync`, `PublishToBrokerAsync`, lectura de outbox y dead letters).
- Consumer: publica al broker (o envía el command que lo produce) y espera con `Eventually.SatisfiesAsync` (polling con timeout), nunca `Thread.Sleep`.
- Idempotencia inbox: publicar dos veces el mismo id de mensaje → un solo efecto.
- En tests de retries, acortar `MessagingOptions.ImmediateRetryDelays`/`ScheduledRetryDelays` en el fixture.
- Prohibido exponer `DbContext` al test de Application; el test de Persistence sí puede usar el context del módulo.

Pendiente de decisión: fixture de mensajería compartido para módulos (hoy `MessagingFixture` y `Eventually` solo existen en `Marketjoya.Common.IntegrationTests`, no en `Marketjoya.TestSupport`).

Plantillas: `templates/infrastructure-repository.md`, `templates/infrastructure-consumer.md`.

## Escenarios mínimos

- [ ] Roundtrip o command que persiste el agregado nuevo.
- [ ] Si hay evento de integración: outbox en el mismo commit.
- [ ] Si hay consumer: idempotencia + un fallo a DLQ o retry, el que el contrato defina.

Checklist: `checklists/infrastructure.md`.
