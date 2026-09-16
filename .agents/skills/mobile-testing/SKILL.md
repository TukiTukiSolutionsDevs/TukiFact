---
name: mobile-testing
description: "Trigger: Android test, test Kotlin, JUnit, Turbine, Room test, Compose UI test, Maestro, E2E Android. Pruebas Android POS/preventa por capa."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "1.4"
---

# Testing Mobile — Market Real

## Activation Contract

Complementa a `mobile-architecture` en `MarketjoyaMobile/`. Activa al escribir o revisar tests Android: JUnit 4, MockK, Turbine, Room, MockWebServer, Compose UI, Koin `verify`, Maestro, fakes de puertos, outbox/sync.

## Hard Rules

- Riesgo de negocio primero (venta, caja, sync, pesaje). No % de cobertura.
- Domain y UseCase: JVM con fakes de `:core:testing`, sin Room ni Retrofit. Fake antes que MockK.
- ViewModel: `runTest` + `MainDispatcherRule` + Turbine. Prohibido `Thread.sleep`, `delay` real, `GlobalScope`.
- Room y MockWebServer: una instancia por clase (`@BeforeClass`/`@AfterClass`); ids aleatorios.
- Puerto con adapter real: contrato abstracto en `:core:testing` que corren fake (JVM) y adapter (`androidTest`).
- Hardware: fakes del puerto cuando existan los puertos; nunca USB real.
- Tests espejan el paquete de producción en minúsculas (`application/completesale/`).
- Errores según `../backend-architecture/references/api-error-contract.md`: aserta `AppResult.Failure` por `type` + `code`, nunca `description` ni excepción. Adapter HTTP cubre la tabla §7 con MockWebServer.
- No se prueba el framework ni lo fiscal.

## Decision Gates

| Cambiaste esto | Cómo | Dónde |
|---|---|---|
| Agregado, VO, regla | JUnit 4 JVM | `src/test` |
| UseCase | Fakes + `runTest` | `src/test` |
| ViewModel | Fakes + `MainDispatcherRule` + Turbine | `src/test` |
| Mapper Room | Round-trip JVM | `src/test` |
| DAO / adapter Room | Contrato + Room in-memory de clase | `src/androidTest` |
| Adapter HTTP | MockWebServer de clase | `src/test` |
| Módulo Koin | `verify` | `app-*/src/test` |
| Screen | `createComposeRule` + `testTag` | `src/androidTest` |
| Viaje de usuario | Maestro | `maestro/` |

## Execution Steps

1. Capa y riesgo (POS vs preventa, online vs outbox).
2. `references/strategy.md` + reference de la capa.
3. Template + fake de `:core:testing`; no mockees el agregado.
4. Checklist de capa; JVM primero, luego instrumentado/Maestro.
5. Sync: mismo `ClientMutationId` ×2 = un efecto.
6. `checklists/verification.md`.

## Output Contract

- Capa, módulo, source set y archivos.
- Fakes vs MockWebServer vs Room.
- Flujos de dinero/sync/hardware cubiertos o hueco explícito.
- Confirmación: Room/servidor no se recrean por `@Test`.
- Comandos ejecutados o no, con motivo.

## References

- [references/strategy.md](references/strategy.md) · [references/conventions.md](references/conventions.md)
- [references/domain.md](references/domain.md) · [references/application.md](references/application.md) · [references/infrastructure.md](references/infrastructure.md)
- [references/compose.md](references/compose.md) · [references/e2e.md](references/e2e.md)
- [templates/](templates/) · [checklists/](checklists/)
- [../mobile-architecture/SKILL.md](../mobile-architecture/SKILL.md) · [../backend-testing/SKILL.md](../backend-testing/SKILL.md)
- [../design-system/SKILL.md](../design-system/SKILL.md) · [../test-e2e/SKILL.md](../test-e2e/SKILL.md)
- [../backend-architecture/references/api-error-contract.md](../backend-architecture/references/api-error-contract.md)
