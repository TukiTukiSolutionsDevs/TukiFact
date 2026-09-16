# Arquitectura Android

Proyecto: `MarketjoyaMobile/`. Reglas completas: `.agents/skills/mobile-architecture/SKILL.md`.

## Módulos

```text
MarketjoyaMobile/
├── app-pos/  app-preventa/          Android apps: Koin, MrTheme, arranque
├── feature/<x>/                     :feature:<x> (Android library), una por feature
├── domain/                          Kotlin/JVM: DomainError, sync (LocalId, ClientMutationId, outbox + puerto)
├── hardware/                        Kotlin/JVM: puertos y estados de periféricos
├── core/
│   ├── common/                      Kotlin/JVM: DispatcherProvider
│   ├── network/                     Kotlin/JVM: OkHttp/Retrofit, X-Correlation-Id, ProblemDetails
│   ├── testing/                     Kotlin/JVM: MainDispatcherRule, fakes, contratos
│   ├── database/                    Android library: Room, outbox
│   └── designsystem/                Android library: tokens generados, MrTheme
├── architecture-tests/              Reglas Konsist
├── build-logic/convention/          Convention plugins + generador de tokens
└── maestro/                         Flujos E2E
```

## Slice dentro de una feature

```text
feature/<x>/src/main/kotlin/pe/marketjoya/feature/<x>/
├── domain/  application/<usecase>/  infrastructure/
└── presentation/  components/  navigation/  di/
```

```text
Presentation -> Application (UseCases) -> Domain <- Ports <- Infrastructure (adapters)
```

Gate CI: `./gradlew verify`.
