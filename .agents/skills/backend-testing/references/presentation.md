# Capa: Presentation

Qué vive aquí: `IEndpoint`, `XRequest`, mapeo a Command/Query, `RequireAuthorization`, `ToHttpResult()`.

No hay reglas de negocio. E2E demuestra el contrato HTTP de los **flujos críticos**, no cada endpoint. El contrato genérico del host (mapping de `Result`, excepciones, auth, correlation, versionado, OpenAPI) ya está cubierto en `Marketjoya.Api.E2ETests` con el módulo `Probe`.

## Qué probar

| Pieza | Comportamiento a demostrar |
|---|---|
| Flujo crítico | Request → 2xx + cuerpo acorde al `Result` |
| Auth | Sin token → 401; sin permiso → 403 |
| Fallo de negocio | ProblemDetails 404/409 con el `code` estático, no un 500 |
| Validación | 400 con `code` `<CasoDeUso>.Validation` y `errors` con `field` y `code` |
| Correlation | `X-Correlation-Id` entra y sale (se genera si no viene) |
| Query string / ruta | Ids y filtros llegan al caso de uso (el mapping no se pierde) |

Flujos críticos (pocos, 5–10 en todo el producto): venta completa, anulación, cierre de caja, recepción de compra, traslado, reserva de pedido, liquidación — más login/autorización de supervisor si el flujo lo exige.

## Qué no probar

- Cada CRUD feliz de un módulo no crítico.
- Re-asertar invariantes del agregado (eso es Domain).
- Volver a probar el contrato genérico del host en cada módulo.

## Dónde

```text
tests/Marketjoya.Api.E2ETests/
└── <Modulo>/
    └── <Submodulo>/
        └── ConfirmSaleTests.cs
```

Espejo vertical. Un test por flujo, no un test por status code suelto si describen el mismo comportamiento (varios asserts sí).

## Cómo

1. `[Collection(ApiCollection.Name)]` + `ApiFixture`: Postgres, Redis, RabbitMQ y Mongo **compartidos por colección**; `ApiFactory : WebApplicationFactory<Program>` configura el host real solo con settings (`UseSetting`) en el entorno `Testing`.
2. Autenticación con tokens reales: `fixture.CreateAuthenticatedClient(TestCaller.Unique(), "<permiso>")` firma HS256 con `sub`, `company`, `warehouse`, `cash_register`, `permissions`. Anónimo: `fixture.CreateAnonymousClient()`.
3. Act: `HttpClient` contra `ApiRoutes.V1 + "/<ruta>"`, pasando `TestContext.Current.CancellationToken`.
4. Assert: status, payload y **efecto** (re-GET o fila en DB) en flujos de dinero/stock.
5. Errores según `../backend-architecture/references/api-error-contract.md` §7: `response.ShouldBeProblemAsync(status)` (valida status, `application/problem+json`, `traceId`, `correlationId`) + `problem.Extension("code")` contra el estático (`<Agregado>Errors.X(...).Code`); no asertes `detail` salvo el texto fijo del 400. En 400: `detail` = "La solicitud tiene errores de validación." y `errors` con `field` (ruta JSON camelCase del body) y `code` de la regla (`WithErrorCode`).
6. El módulo debe estar en `ModuleCatalog` para que sus endpoints existan en el host de test.
7. No uses `Thread.Sleep` para esperar side effects; polling con timeout.

Mapeo `ErrorType` → status del host (cada categoría con su status, `code`, `traceId`, `correlationId`; `code` ausente solo en respuestas del framework y 5xx por excepción, ver contrato §3): una vez en `Marketjoya.Api.E2ETests` con el módulo `Probe`, no por módulo. El status es contrato: la categoría no viaja y los clientes la derivan de él. El fallback `Validation.<Regla>` (regla sin `WithErrorCode`) y `Validation.Custom` se prueban una vez en Common (`tests/Marketjoya.Common.UnitTests/Application/Behaviors/ValidationBehaviorTests.cs`), no por módulo. El 400 con `errors` del host lo cubre `tests/Marketjoya.Api.E2ETests/Errors/ValidationProblemTests.cs`.

Pendiente de decisión: cómo sustituye `ApiFactory` los externos (SUNAT/IA/GPS); hoy no expone un hook para reemplazar servicios.

Plantilla: `templates/presentation-endpoint.md`.

## Escenarios mínimos por flujo E2E nuevo

- [ ] Happy path autenticado.
- [ ] 401/403.
- [ ] Un fallo de negocio visible en HTTP (stock, crédito, caja) con su `code`.
- [ ] Si el request tiene campos validados: 400 con `errors` (`field`, `code`).

Checklist: `checklists/presentation.md`.
