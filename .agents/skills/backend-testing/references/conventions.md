# Convenciones de escritura de tests

## Nombre

`Metodo_Escenario_ResultadoEsperado`

```text
Reserve_InsufficientStock_ReturnsFailure
Send_CommandReturnsFailure_RollsBackWithoutDispatchingEvents
Consume_SameMessageDeliveredTwice_HandlesItOnce
```

El método es el del tipo bajo prueba (dominio) o el del caso de uso (handler). El escenario es negocio, no mecánica (`WhenMockReturnsNull` está prohibido).

## AAA

Comentarios `// Arrange`, `// Act`, `// Assert`. Un test = un comportamiento. Varios asserts sí, si describen el mismo hecho.

`[Theory]` cuando cambian datos, no cuando cambian comportamientos (esos van en `[Fact]` separados).

## Forma

- Clases `public sealed`; fixture de colección por primary constructor: `public sealed class XTests(PostgresFixture fixture)`.
- `[Collection(<X>Collection.Name)]` con la constante `Name` de la definición de colección.
- Cancelación: `TestContext.Current.CancellationToken` (xUnit v3), no `CancellationToken.None`.

## Datos

- Test data builders (`SaleBuilder`, `StockItemBuilder`) + Bogus.
- Nunca armar el agregado campo por campo en cada test.
- Seed fija (`Randomizer.Seed`) si el test debe ser determinista.
- Builders viven en `UnitTests/<Submodulo>/Builders/` y se reutilizan desde IntegrationTests del mismo módulo (referencia de proyecto a los builders, no a las clases de test).

## Aserciones

FluentAssertions 7.x en todos los tests. `.Because("...")` o el argumento `because` cuando el motivo no se lee del nombre.

Aserta `Error.Code` y `Error.Type` contra el estático de dominio (`UserErrors.NotFound(id).Code`), no contra un string suelto ni contra `Description`. En validación, `FieldErrors` por `Field` y `Code`. En HTTP, `code` y `errors` del ProblemDetails (contrato `../backend-architecture/references/api-error-contract.md` §7).

## Aislamiento

- Unitarias: sin IO, orden independiente.
- Integración/E2E: DB compartida de la colección. Cada test usa `Guid`/emails únicos. No dependas del orden ni de una tabla vacía. No limpiar por `Fact`.
- Prohibido `Thread.Sleep`. Espera con polling y timeout (`Eventually.SatisfiesAsync` en integración de mensajería).
- Prohibida lógica `if` dentro del test; usa `[Theory]` o tests separados.

## Mocks

NSubstitute solo en el borde externo (SUNAT, OCR, GPS). El resto es Testcontainers, fakes de dominio in-memory en unitarias o dobles registrados en el fixture (`ConfigureTestServices`).
