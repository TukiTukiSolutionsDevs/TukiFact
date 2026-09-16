# Mapa de arquitectura Android

POS y Preventa son clientes del monolito modular `.NET`. Proyecto Gradle: `MarketjoyaMobile/`. Capas equivalentes a `backend-architecture`.

## Toolchain e identidad

| Tema | Valor |
|---|---|
| Build | Gradle 9.7.1, AGP 9.4 con Kotlin integrado (sin plugin `kotlin-android`), Kotlin 2.4.20 |
| JVM | Toolchain 21 (`jvmTarget` del catálogo). Si `java` no está en el PATH, exportar `JAVA_HOME` al JBR de Android Studio o a un JDK 21 |
| SDK | `compileSdk`/`targetSdk` 37, `minSdk` 24 |
| applicationId | `marketjoya.applicationIdPrefix=pe.marketjoya` (`gradle.properties`) → `pe.marketjoya.pos`, `pe.marketjoya.preventa` |
| Namespace / paquete | Derivado de la ruta del módulo: `:core:network` → `pe.marketjoya.core.network`, `:app-pos` → `pe.marketjoya.pos` |
| Versiones | Solo `gradle/libs.versions.toml` |

## Módulos

| Módulo | Tipo (convention plugin) | Contenido actual |
|---|---|---|
| `:app-pos`, `:app-preventa` | Android app (`marketjoya.android.application` + `compose`) | `Application` con `startKoin`, `MainActivity` con `MrTheme`, `di/*Modules.kt` |
| `:feature:<x>` | Android library (`marketjoya.android.feature`) | Ninguna todavía; ver `MarketjoyaMobile/feature/README.md` |
| `:domain` | Kotlin/JVM (`marketjoya.jvm.library`) | `error/` (`AppError`, `ErrorType`, `FieldError`, `AppResult`), `sync/` (`LocalId`, `ClientMutationId`, `PendingSyncOperation`, `PendingSyncWriter`, `SyncAttemptPolicy`) |
| `:hardware` | Kotlin/JVM | `DeviceConnectionState` |
| `:core:common` | Kotlin/JVM | `DispatcherProvider`, `commonModule` (Koin) |
| `:core:network` | Kotlin/JVM | OkHttp/Retrofit, `CorrelationIdInterceptor`, `CorrelatedIOException`, `error/HttpErrorMapper`, `error/ProblemDetails` |
| `:core:testing` | Kotlin/JVM | `MainDispatcherRule`, `TestDispatcherProvider`, fakes y contratos de puertos |
| `:core:database` | Android library | Room `MarketjoyaDatabase`, outbox, `databaseModule` |
| `:core:designsystem` | Android library + `marketjoya.design.tokens` | Tokens generados (`generated/`), `MrTheme` |
| `:architecture-tests` | Kotlin/JVM | Reglas Konsist + fixtures |
| `build-logic/convention` | Build incluido | 8 convention plugins + generador de tokens |

Convention plugins: `android.application`, `android.library`, `android.compose`, `android.feature`, `jvm.library`, `unit.testing`, `quality`, `design.tokens` (prefijo `marketjoya.`).

## Capas

| Capa | Dónde vive | Prohibido |
|---|---|---|
| Domain | `:domain` (compartido) o `feature/<x>/domain/` | Android, Room, Retrofit, OkHttp, Compose, USB, BT |
| Application | `feature/<x>/application/<usecase>/` | `Context`, DAO, Room, navegación |
| Infrastructure | `feature/<x>/infrastructure/`, `:core:database`, `:core:network` | Reglas de negocio, UI |
| Presentation | `feature/<x>/presentation/`, `components/`, `navigation/` | IO, hardware, contratos HTTP crudos |

## Slice de feature

```text
feature/<x>/src/main/kotlin/pe/marketjoya/feature/<x>/
├── domain/                 ← solo lenguaje propio de la feature
├── application/
│   └── completesale/       ← un UseCase por paquete: CompleteSaleUseCase.kt
├── infrastructure/         ← adapters Retrofit/Room + mappers
├── presentation/           ← <X>Route, <X>Screen, <X>ViewModel
├── components/             ← Compose de feature sobre Mr*
├── navigation/
└── di/                     ← módulo Koin, registrado en di/*Modules.kt de la app
```

Paquetes en minúsculas (convención Kotlin). `:architecture-tests` falla si una capa no es una de las siete o si el UseCase no está en `application.<nombre en minúsculas>`.

## Flujo

```text
Screen -> ViewModel -> CompleteSaleUseCase -> SaleWriter (port)
     -> RetrofitSaleWriter (adapter) -> POST /api/v1/... (Command backend)
     <- AppResult<T> (Success | Failure(AppError)) <- ViewModel <- UiState / Mr*
```

El UseCase retorna `AppResult<T>`; `HttpErrorMapper` es el único que traduce ProblemDetails → `AppError` (`references/domain-and-use-cases.md`, `references/backend-coupling.md`).

Hardware entra por un puerto de `:hardware`, nunca desde el ViewModel parseando bytes. Contrato HTTP: `references/backend-coupling.md`.
