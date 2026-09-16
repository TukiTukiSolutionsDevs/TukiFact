# Estrategia Android

Misma pirámide que backend: unitario de dominio, integración de adapters, E2E pocos.

```text
        ╱ Maestro ╲           ← venta, sync pedido, turno (hoy solo smoke)
       ╱           ╲
      ╱  Integración ╲        ← Room androidTest + MockWebServer + Koin verify
     ╱                 ╲
    ╱     Unitarias JVM  ╲   ← Domain, UseCase, ViewModel + fakes
```

## Source sets

```text
feature/sales/src/
├── test/kotlin/pe/marketjoya/feature/sales/        ← JVM: domain, usecase, vm, MockWebServer
└── androidTest/kotlin/pe/marketjoya/feature/sales/ ← Compose UI, Room en SQLite real
core/testing/src/main/kotlin/pe/marketjoya/core/testing/ ← fakes y contratos compartidos
maestro/
├── pos-smoke.yaml
└── preventa-smoke.yaml
```

## Stack (catálogo `gradle/libs.versions.toml`)

| Propósito | Librería | Cómo llega |
|---|---|---|
| JVM | JUnit 4.13.2 (`useJUnit()`), `kotlinx-coroutines-test` | `marketjoya.unit.testing` |
| Flows | Turbine 1.2.1; `StateFlow.value` si solo importa el estado actual | `marketjoya.unit.testing` |
| Mock puntual | MockK 1.14.11 (`coEvery`) solo API difícil de fakear | `marketjoya.unit.testing` |
| Main / dispatchers | `MainDispatcherRule`, `TestDispatcherProvider` | `:core:testing` |
| Fakes / contratos | `FakePendingSyncWriter`, `PendingSyncWriterContract` | `:core:testing` |
| HTTP | MockWebServer 5.5 (`mockwebserver3`), compartido | `testImplementation(libs.okhttp.mockwebserver)` |
| Room | `Room.inMemoryDatabaseBuilder` una vez por clase | `androidTest` de `:core:database` |
| Compose | `ui-test-junit4` + `testTag` | `marketjoya.android.compose` (`androidTest`) |
| DI | `koin-test` `verify` | `app-*/src/test` |
| E2E | Maestro (no instalado por defecto) | `maestro/README.md` |
| Arquitectura | Konsist 0.17.3 | `:architecture-tests` |

No `TestCoroutineDispatcher` (deprecado). Robolectric no está en el stack.

## Obligatorio

POS: turno, carrito no descuenta stock, pago Unknown, idempotencia, peso estable. Preventa: persistir pedido + `LocalId` antes de sync, conflicto visible. Hardware: fake, nunca USB real en CI. Estos casos se escriben con sus features (hoy no existen).

## Rendimiento

| Momento | Qué |
|---|---|
| `@BeforeClass` (companion `@JvmStatic`) | Room in-memory, `MockWebServer().start()` |
| Cada `@Test` | Ids únicos (`LocalId.random()`, `ClientMutationId.random()`, `UUID.randomUUID()`) |
| `@AfterClass` | Cierra servidor/DB una vez |

Prohibido crear la base in-memory en cada test. Prohibido asertar conteos globales de tabla; aserta por id.
