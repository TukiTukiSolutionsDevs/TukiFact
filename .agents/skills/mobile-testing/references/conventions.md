# Convenciones Android

- Nombre: `metodo_escenario_resultado` (ej. `enqueue_sameMutationTwice_keepsSingleEffect`).
- AAA. Un `@Test` = un comportamiento.
- Paquete del test = paquete de producción, en minúsculas: `pe.marketjoya.feature.sales.application.completesale.CompleteSaleUseCaseTest`.
- Fakes compartidos en `:core:testing` (`pe.marketjoya.core.testing.<área>`), ej. `sync/FakePendingSyncWriter`. Fake local de feature solo si nadie más lo usa. MockK excepcional.
- Contrato de puerto: clase abstracta `<Port>Contract` en `:core:testing`; `Fake<Port>Test` (JVM) y `Room<Port>Test` (`androidTest`) la extienden.
- `runTest`; dispatchers de test (`MainDispatcherRule`, `TestDispatcherProvider`). Prohibido `runBlocking` + `sleep`.
- Isolation: ids aleatorios (`LocalId.random()`, `ClientMutationId.random()`), no conteos globales.
- Room/MockWebServer: lifecycle de clase, no de método.
- detekt aplica a `src/test` y `src/androidTest` (excepto `MagicNumber`); ktlint vía Spotless también.
- Prohibido `GlobalScope`, Activity en singleton, logs con token.
