# Capa: Application

Qué vive aquí: commands/queries, handlers, validators FluentValidation, `IDomainEventHandler<T>`, puertos en `Abstractions/`.

El handler orquesta; las reglas ya se probaron en Domain. Aquí se prueba que el caso de uso **committea de verdad** contra Postgres.

## Qué probar

| Pieza | Comportamiento a demostrar |
|---|---|
| Command handler | Éxito: agregado persistido, `Result.Success`, eventos de dominio despachados en el mismo commit |
| Command handler | Fallo de negocio: `Result.Failure` con `Error.Code` y `Error.Type` del estático, sin side effects persistidos |
| Command inválido | `Error.Type` `Validation`, `Error.Code` `<CasoDeUso>.Validation` y `FieldErrors` con `Field` y `Code`, sin mutación |
| Query handler | SQL del schema propio devuelve el `Response`; not found → `*Errors.NotFound` |
| Idempotencia | Mismo `client_mutation_id` dos veces → una sola mutación |
| Handler de dominio | Traduce a integración y el outbox tiene el mensaje (ver Infrastructure) |
| Autorización de aplicación | Ticket/supervisor rechazado no muta (si el caso de uso lo tiene) |

Se envía el mensaje con `ISender` (`fixture.SendAsync`), no se instancia el handler a mano con mocks.

## Qué no probar

- Handler con `NSubstitute` de `I*Repository`: no demuestra commit, interceptores ni outbox.
- Cada regla trivial del validator por separado (`NotEmpty`, `EmailAddress`): basta un test de command inválido que aserte sus `FieldErrors`.
- Lógica que solo vive en el agregado (eso es Domain).

## Dónde

```text
tests/Marketjoya.Modules.<M>.IntegrationTests/
├── <M>IntegrationFixture.cs            ← : ModuleIntegrationFixture
├── <M>Collection.cs                    ← ICollectionFixture<<M>IntegrationFixture>
└── <Submodulo>/
    ├── CreateUser/
    │   └── CreateUserCommandHandlerTests.cs
    └── GetUserById/
        └── GetUserByIdQueryHandlerTests.cs
```

Carpeta espejo del caso de uso. Colección xUnit compartida: **un** Postgres, migrado **una vez** al iniciar la colección (`references/strategy.md`).

## Cómo

1. Fixture de colección derivado de `ModuleIntegrationFixture` (`Marketjoya.TestSupport`): `protected override IModule Module { get; } = new <M>Module();`. Registra Common y el módulo como el host y migra una vez.
2. Dobles en `ConfigureTestServices` (usar `Replace` para servicios que Common agrega con `TryAdd`, p. ej. `TimeProvider`); configuración extra en `Settings(...)`. Externos (SUNAT/IA/GPS) → NSubstitute ahí.
3. Arrange: seed **del test** con builders e ids únicos.
4. Act: `await fixture.SendAsync(command)`; `configureScope` para dobles scoped (p. ej. usuario actual).
5. Assert: `Result` + **tu** fila (helper de lectura del fixture vía `I<M>ReadDbConnection` o query por id). No asertas conteos globales. No llames `SaveChanges`.
   Fallos: `Error.Code` y `Error.Type` contra el estático; en validación, `FieldErrors` (`Field` camelCase, `Code`). Nunca `Description` (contrato `../backend-architecture/references/api-error-contract.md`).
6. Commands pasan por `TransactionBehavior`; el test no abre otra transacción envolvente.
7. Queries: sin transacción; aserta columnas/aliases del `Response` de la fila sembrada en ese test.

Pendiente de decisión: seed de catálogos por colección (roles, almacenes). `ModuleIntegrationFixture` no expone hook de seed (`InitializeAsync` no es virtual).

Plantillas: `templates/application-command.md`, `templates/application-query.md`.

## Escenarios mínimos por caso de uso nuevo

- [ ] Éxito persistido (command) o fila mapeada (query).
- [ ] Fallo de negocio sin mutación (`Error.Code` + `Error.Type`).
- [ ] Si tiene validator: command inválido con `FieldErrors`.
- [ ] Si el contrato de sync aplica: `client_mutation_id` duplicado.
- [ ] Si emite integración: outbox en el mismo commit (`references/infrastructure.md`).

Checklist: `checklists/application.md`.
