# Estrategia de pruebas

La cobertura se mide por riesgo de negocio, no por porcentaje. No se persigue 80% global: se persigue que lo que rompe plata o cuadre de caja esté blindado antes de producción. El reporte de cobertura es informativo; no hay umbral.

## Pirámide

```text
        ╱ E2E ╲              ← pocos, solo flujos críticos (HTTP)
       ╱ 5-10  ╲               WebApplicationFactory + Testcontainers
      ╱ Integración ╲         ← columna vertebral (~60-70%)
     ╱               ╲          handlers y adapters contra infra real
    ╱    Unitarias     ╲      ← dominio puro (~25-35%), sin IO
   ╱                    ║     + Architecture tests (CI, siempre)
  ╱______________________╲
```

Integración es la columna porque el handler orquesta; mockear el repositorio no prueba el commit, el outbox ni el SQL.

## Proyectos

```text
MarketjoyaBackend/tests/
├── Directory.Build.props                          ← configuración única de todos los tests
├── Marketjoya.Modules.<M>.UnitTests/              ← Domain
├── Marketjoya.Modules.<M>.IntegrationTests/       ← Application + Infrastructure
├── Marketjoya.Common.UnitTests/                   ← Common.Domain / Application (una vez)
├── Marketjoya.Common.IntegrationTests/            ← Common.Infrastructure (una vez)
├── Marketjoya.ArchitectureTests/                  ← fronteras (NetArchTest)
├── Marketjoya.Api.E2ETests/                       ← contrato HTTP del host, flujos críticos
└── Marketjoya.TestSupport/                        ← fixtures compartidos (librería, no es test)
```

- `tests/Directory.Build.props`: todo proyecto cuyo nombre termina en `Tests` es una app de test xUnit v3 (`OutputType` Exe) con `xunit.v3`, `FluentAssertions`, `coverlet.MTP` y `using` globales de `Xunit` y `FluentAssertions`. El resto (`Marketjoya.TestSupport`) es librería.
- Todo proyecto de test debe estar en `MarketjoyaBackend.sln` (lo verifica un architecture test).
- Proyecto de módulo sin tests todavía: `--ignore-exit-code 8` en `TestingPlatformCommandLineArguments` (ver proyectos de Identity) hasta el primer test.

Los tests espejan el vertical slice. Ejemplo: `Marketjoya.Modules.Users.UnitTests/Users/UserTests.cs`, `Marketjoya.Modules.Users.IntegrationTests/Users/CreateUser/CreateUserCommandHandlerTests.cs`.

## Ciclo de vida de la DB (rendimiento)

Crear y limpiar el schema en cada prueba es el mayor coste de la suite. Se hace **una vez por colección**.

| Momento | Qué ocurre |
|---|---|
| `InitializeAsync` del fixture de colección | Sube Testcontainers, registra servicios y migra **una vez** |
| Cada `[Fact]` | Inserta filas con identidad única. No crea DB, no migra, no limpia, no `DROP` |
| `DisposeAsync` del fixture | Apaga el contenedor |

`ModuleIntegrationFixture` (`Marketjoya.TestSupport`) implementa esto para módulos: un Postgres, `AddCommonApplication` + `AddCommonPersistence` + `IModule.Register` como el host, `ConfigureTestServices` para dobles, migraciones de cada `BaseDbContext` una vez y `SendAsync` en un scope por envío.

Prohibido: `IAsyncLifetime` por clase de test que resetee el schema; migrar en el constructor; limpiar al inicio de cada `Fact`.

Aislamiento = datos únicos (`Guid.NewGuid()`, email/`client_mutation_id` por test), no tabla vacía. Aserta **tu** fila por id. Prohibido `HaveCount(1)` o “el único usuario en la tabla”.

## Librerías fijas

| Propósito | Librería | Uso |
|---|---|---|
| Runner | Microsoft.Testing.Platform (`global.json`) | `dotnet test` en local y CI |
| Framework | xUnit v3 4.0.1 | `Fact` / `Theory`; colecciones para compartir contenedores; `TestContext.Current.CancellationToken` |
| Aserciones | FluentAssertions 7.2.2 | obligatorio; 7.x es la última línea Apache-2.0 (8+ comercial) |
| Cobertura | coverlet.MTP 10.0.1 | Cobertura, informativa |
| Infra | Testcontainers 4.15.0 | imágenes de `ContainerImages`: `postgres:16-alpine`, `redis:7-alpine`, `rabbitmq:3.13-management-alpine`, `mongo:7`; **un contenedor por colección** |
| HTTP | Microsoft.AspNetCore.Mvc.Testing (`WebApplicationFactory`) | E2E + mismos contenedores |
| Arquitectura | NetArchTest.Rules 1.3.2 | CI, bloquea el merge |
| Datos | Bogus + test data builders | seed fija cuando el test debe ser determinista (Bogus aún no está en `Directory.Packages.props`) |
| Mocks | NSubstitute | solo SUNAT, OCR, GPS (aún no está en `Directory.Packages.props`) |

Respawn no se usa: los fixtures migran una vez y los tests se aíslan por datos únicos.

## Comandos

Desde `MarketjoyaBackend/` (integración y E2E requieren Docker):

| Objetivo | Comando |
|---|---|
| Todas las suites | `dotnet test` |
| Un proyecto | `dotnet test --project tests/Marketjoya.ArchitectureTests` |
| Una clase | `dotnet test --project tests/Marketjoya.Common.UnitTests --filter-class "*.MediatorTests"` |
| Como CI | `dotnet build -c Release && dotnet test -c Release --no-build` |
| Cobertura | `dotnet test --coverlet --coverlet-output-format cobertura --results-directory TestResults` |

## Obligatorio antes de producción

Invariantes: `StockItem` (reserva/salida/ajuste), `Sale` (pagos, descuentos con doble llave), `CreditLine` (tope y vencimiento), `CashClosing` (arqueo), motor multiempresa (`ICompanyDeterminator` + splitter), conversiones de granel (saco ↔ gramos).

Casos de uso de dinero y stock: venta completa, anulación, cierre de caja, recepción de compra, traslado, reserva de pedido, liquidación.

Contratos de consistencia:

- Idempotencia de sync: mismo `client_mutation_id` dos veces → mismo resultado, una sola mutación.
- Outbox: el evento de integración queda persistido en el mismo commit que el agregado.
- Consumers: inbox idempotente, retry y DLQ.
- Architecture tests verdes en CI.

## No se prueba

- DTOs, mappers triviales, validadores de formato obvios (salen en integración).
- Common ya cubierto en `Marketjoya.Common.*Tests` y `Marketjoya.Api.E2ETests`.
- El framework (EF Core, Wolverine, ASP.NET).

## Mapa capa → pirámide

| Capa | Unidad de prueba | Pirámide |
|---|---|---|
| Domain | agregado / VO / regla / spec | Unitaria |
| Application | caso de uso vía `ISender` | Integración |
| Infrastructure | adapter (repo, SQL, consumer) | Integración |
| Presentation | HTTP del flujo crítico | E2E |
| Common | primitiva transversal | Unitaria, integración o E2E del host, una vez |
| Fronteras | regla NetArchTest | Architecture tests |

Detalle operativo: `MarketjoyaBackend/docs/testing.md`.
