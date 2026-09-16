# Errores y Result

Vista backend del [contrato de error unificado](api-error-contract.md). Si algo aquí contradice el contrato, manda el contrato.

## Modelo

- `public sealed record Error(string Code, string Description, ErrorType Type)` + `FieldErrors` (lista de `FieldError`, vacía salvo en validación). Fábricas `Error.Validation/NotFound/Conflict/Failure(code, description)`; `Error.None` solo en éxito.
- `public sealed record FieldError(string Field, string Code, string Description)`.
- `ErrorType` (PascalCase): `Validation`, `NotFound`, `Conflict`, `Failure`. `Unauthorized` y `Forbidden` los emite el framework, no un `Result`. Lista cerrada: agregar una categoría es cambio de contrato (§2).
- `ErrorType` no viaja en el cable: los clientes derivan la categoría del `status`. Por eso el status de cada `ErrorType` es parte del contrato y no se cambia.
- `Result.Success()`, `Result.Success(value)`, `Result.Failure(error)`, `Result.Failure<T>(error)`. Leer `Value` de un failure lanza.

## Código (`Error.Code`)

- Formato `<Ámbito>.<Motivo>` en PascalCase: `Sale.NotFound`, `CashRegister.AlreadyOpen` (contrato §3).
- Es API pública: no se renombra ni se reutiliza con otro significado. Deprecar = dejar de emitir.
- Estáticos por agregado en Domain: `<Agregado>Errors` (`SaleErrors.NotFound(id)`). Nunca `Error` inline.
- Códigos reservados (`<UseCase>.Validation`, `BusinessRule.<Regla>`, `Concurrency.Conflict`): tabla en contrato §3; ningún `<Agregado>Errors` los reutiliza.

## Dónde vive cada fallo

Status HTTP por categoría: contrato §2.

| Fallo | Mecanismo | `ErrorType` → HTTP | `code` | `detail` |
|---|---|---|---|---|
| No encontrado, conflicto previsible | `Result.Failure(<Agregado>Errors.X)` | `NotFound` 404 / `Conflict` 409 | `Error.Code` | `Error.Description` |
| Formato/presencia del request | Validator FluentValidation → `ValidationBehavior` | `Validation` 400 | `<UseCase>.Validation` | fijo "La solicitud tiene errores de validación."; detalle en `errors` |
| Fallo controlado genérico | `Error.Failure(...)` | `Failure` 500 | `Error.Code` | texto genérico (oculta `Description`) |
| Invariante de dominio rota | `CheckRule` → `BusinessRuleValidationException` | `Conflict` 409 | `BusinessRule.<Regla>` | mensaje de la regla |
| `sync_version` desactualizado | `DbUpdateConcurrencyException` | `Conflict` 409 | `Concurrency.Conflict` | texto genérico |
| Infra caída, bug | excepción no controlada | 500 | ausente (el cliente aplica el reservado de §3) | ninguno (texto solo en Development) |
| JSON mal formado / sin token / sin permiso / ruta o versión inexistente | framework | 400 / 401 / 403 / 404 | ausente (el cliente aplica el reservado de §3) | — |

Excepciones solo para lo excepcional; nunca para formato ni reglas previsibles.

## Validación (`ValidationBehavior`)

- Si el validator falla, devuelve un único `Error.Validation("<UseCase>.Validation", "La solicitud tiene errores de validación.", fieldErrors)` (texto fijo) con un `FieldError` por fallo, sin invocar handler ni abrir transacción.
- `FieldError.Field` = `PropertyName` de FluentValidation convertido a ruta JSON camelCase del body del request HTTP, por segmento: `Items[0].Quantity` → `items[0].quantity`.
- `FieldError.Code` = `ErrorCode` de la regla. Cada regla declara `.WithErrorCode("<Campo>.<Regla>")` (`Quantity.GreaterThanZero`) y ese código se conserva. Si falta, FluentValidation reporta el nombre del validador (`NotEmptyValidator`, sin punto) y `ValidationBehavior` emite `Validation.<Regla>` (`Validation.NotEmpty`); nunca el código crudo de FluentValidation. Una falla sin código (`AddFailure` dentro de `Custom`) se emite como `Validation.Custom`.
- `FieldError.Description` = `ErrorMessage`.
- El Command usa los mismos nombres de propiedad que el Request; si difieren, `field` no apunta al body (contrato §4).

## Presentation (ProblemDetails)

- `ToHttpResult()`: `Result` éxito → 204; `Result<T>` éxito → 200 con el valor; failure → ProblemDetails con el status de su `ErrorType`.
- Ningún endpoint construye `ProblemDetails` a mano.
- Cuerpo `application/problem+json` (RFC 9457) según contrato §4: `type` (URI del RFC, no la categoría), `title`, `status`, `detail` + extensiones `code`, `traceId`, `correlationId` (= header `X-Correlation-Id`) y, solo en 400, `errors` (`[{ field, code, description }]`). Todo 400 de `Validation` incluye `errors`, aunque sea lista vacía (p. ej. `Error.Validation` devuelto directo por un handler).
- `code` obligatorio en todo error que emite la aplicación (negocio, validación, reglas, concurrencia). Solo falta en respuestas del framework y en 5xx por excepción no controlada; el cliente aplica el código reservado por status (contrato §3, §6.1). No se replica aquí esa lista.
- En `Failure` el `detail` no expone `Error.Description`.
- `.WithStandardProblems()` documenta 400/404/409/500 en OpenAPI; el schema ProblemDetails declara `errors`.
- `BusinessRuleValidationExceptionHandler` vive en `Common.Presentation`; `DbUpdateConcurrencyExceptionHandler` en `Common.Infrastructure` (Presentation no referencia EF Core).

## Estado de implementación

El contrato está implementado (§8), sin pendientes backend:

- `FieldError` y `Error.FieldErrors` en `Common.Domain/Results`; fábrica `Error.Validation(code, description, fieldErrors)`.
- `ValidationBehavior` + `ValidationFieldErrors` en `Common.Application/Behaviors`.
- `ErrorProblem` en `Common.Presentation/Results` (extensión `errors`); `ProblemDetailsSchemaTransformer` en `Api/OpenApi`.
- Pruebas de referencia: `tests/Marketjoya.Common.UnitTests/Application/Behaviors/ValidationBehaviorTests.cs` y `tests/Marketjoya.Api.E2ETests/Errors/ValidationProblemTests.cs`.

## Pendiente de decisión

- Convención de creación (201 + `Location`): `ToHttpResult()` no la produce.

Primitivas: `Common.Domain/Results`. Configuración: `MarketjoyaBackend/docs/configuration.md` (Errors). Ver `references/common-shared-kernel.md`.
