# Dependencias Android

```text
Presentation → Application → Domain
Infrastructure → Application → Domain
```

## Por construcción (Gradle)

- `:domain`, `:hardware`, `:core:common`, `:core:network`, `:core:testing` usan `marketjoya.jvm.library`: sin plugin Android, no resuelven AndroidX.
- `marketjoya.android.feature` añade `:domain`, `:core:common`, `:core:designsystem`, Koin, lifecycle y navigation. `:core:network`, `:core:database` o `:hardware` se añaden solo si el slice los usa.
- Versiones solo en `gradle/libs.versions.toml`.

## Reglas Konsist (`:architecture-tests`, Konsist 0.17.3)

| Regla | Qué falla |
|---|---|
| `DomainPurity` | `:domain`, `:hardware`, `feature/<x>/domain/` o `feature/<x>/application/` importan `android.`, `androidx.`, `retrofit2.`, `okhttp3.` |
| `KotlinResultReturn` | Mismo alcance que `DomainPurity`: función que devuelve `kotlin.Result` (se usa `AppResult`) |
| `UseCaseDependencies` | `*UseCase` con `Context`, `androidx.navigation.`, `androidx.room.` o `*Dao` (import o constructor) |
| `ViewModelDependencies` | `*ViewModel` con USB, Bluetooth, Room, Retrofit, OkHttp o `*Dao` |
| `ComposableInfrastructure` | Archivo con `@Composable` que importa `.infrastructure.`, Retrofit, OkHttp, Room, USB, BT o `*Dao` |
| `PackageMirrorsModule` | Paquete que no empieza por el namespace del módulo |
| `FeatureSliceLayer` | Capa de feature fuera de `domain/application/infrastructure/presentation/components/navigation/di` |
| `UseCaseFolder` | `XUseCase` fuera de `<feature>.application.x` (minúsculas) |
| `VisualLiterals` | `Color(0x…)` o `<n>.dp`/`<n>.sp` en `main` fuera de `core/designsystem/.../generated` |

Cada regla tiene fixtures en `architecture-tests/fixtures/violations/` (deben reportarse) y `fixtures/valid/` (deben pasar). El código real de los módulos también debe pasar. Una regla nueva entra con su fixture de violación.

## Operativas (revisión)

1. UseCase recibe puertos (`SaleWriter`, `PendingSyncWriter`), no clases concretas.
2. DTO Retrofit ≠ Entity Room ≠ Domain ≠ UiModel. Mappers explícitos en Infrastructure.
3. Puerto de escritura con techo corto: no `getAll()` ni repositorio genérico (mismo criterio que el backend).
4. Listados: puerto de lectura local o GET del API, no hidratar agregados.
5. Mutación sincronizable sin `ClientMutationId` = bloqueador.

## Herramientas de calidad

| Herramienta | Cómo corre |
|---|---|
| detekt 1.23.8 | Tarea `detekt` por CLI (el plugin Gradle 1.x no es compatible con AGP 9); `config/detekt/detekt.yml`, sin baseline |
| ktlint 1.8.0 | Spotless 8.10.2 (`spotlessCheck` / `spotlessApply`) |
| Android Lint | `warningsAsErrors`, `abortOnError` |
| Tokens | `checkDesignTokens` en `check` de `:core:designsystem` |
| Gate CI | `./gradlew verify` = `check` de todos los módulos + tests de `build-logic` + APKs debug |
