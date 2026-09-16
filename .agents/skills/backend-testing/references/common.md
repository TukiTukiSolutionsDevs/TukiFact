# Capa: Common

Mecanismos transversales. Se prueban **una vez** en proyectos de Common (o del host), no se re-prueban en cada módulo.

## Qué está cubierto

| Pieza | Qué se demuestra | Dónde |
|---|---|---|
| Domain | `Result`/`Result<T>`, igualdad y `CheckRule` de `Entity`, eventos de `AggregateRoot`, igualdad de `ValueObject` | `Marketjoya.Common.UnitTests/Domain` |
| Mediador | Send/Publish, orden de behaviors, notificaciones y eventos de dominio | `Common.UnitTests/Application/Messaging` |
| Behaviors | Validation (sin handler ni transacción; `detail` fijo "La solicitud tiene errores de validación."; `FieldErrors` con `field` camelCase y código de `WithErrorCode` o fallback `Validation.<Regla>`), Transaction (commit/rollback/relanza; queries sin transacción), Telemetry (activity y tags) | `Common.UnitTests/Application/Behaviors` |
| DI y reloj | `AddCommonApplication` registra behaviors en orden; `Clock` (`UtcNow`, `LimaNow`) | `Common.UnitTests/Application` |
| Persistencia | snake_case y `__EFMigrationsHistory` en el schema; `TransactionManager` (commit, rollback, handler que lanza); interceptores de eventos, auditoría y `sync_version`; `ReadDbConnection` con `DateOnly`/`timestamptz`; sin retry strategy | `Marketjoya.Common.IntegrationTests/Persistence` |
| Mensajería | outbox tras commit y descarte en rollback; inbox idempotente; retries + dead letter en Mongo; correlation id en el consumer | `Common.IntegrationTests/Messaging` |
| Host HTTP | `ToHttpResult()` 200/400/404/409/500 (500 sin descripción; 400 con `errors`), cada `ErrorType` con su status, `code`, `traceId`, `correlationId` (contrato §7); excepciones → 409/500; auth 401/403; `X-Correlation-Id`; versionado 404; OpenAPI y deriva; health; validación de arranque | `Marketjoya.Api.E2ETests` |

## Módulos de prueba

- `Marketjoya.Common.IntegrationTests` usa un módulo `Shipping` de prueba (`Persistence/Fakes`: `ShippingModule`, `ShippingDbContext`, repositorio, read connection, migraciones) sobre `PostgresFixture : ModuleIntegrationFixture`, y `MessagingFixture` (Postgres + RabbitMQ + Mongo + host Wolverine).
- `Marketjoya.Api.E2ETests` usa un módulo `Probe` de prueba (`Fakes/Probe`) registrado por `ApiFactory`; queda fuera del contrato OpenAPI commiteado.

## Qué no probar

- Cada módulo volviendo a testear el mediador, los behaviors o `ToHttpResult()`.
- Instrumentación de OpenTelemetry del host (se cubre por behaviors; no mockees `Activity` en cada caso de uso).
- EF Core, Wolverine o ASP.NET como productos.

## Cómo

Si un bug parece de “Common” pero solo se reproduce en un módulo, primero escribe el test en el módulo (Application/Infrastructure). Extrae a Common solo cuando el arreglo sea transversal y mueve el test con el arreglo.

Los módulos asumen Common verde. Un caso de uso nuevo no añade tests de `ValidationBehavior`.

Integración de Common usa la misma regla de DB: un contenedor por colección, sin create/limpieza por test (`references/strategy.md`).
