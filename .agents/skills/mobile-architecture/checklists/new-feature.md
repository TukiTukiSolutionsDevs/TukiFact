# Checklist: nueva feature Android

- [ ] Command/Query del API identificado (`references/backend-coupling.md`); POS, preventa o ambas.
- [ ] Módulo `:feature:<x>` incluido en `settings.gradle.kts` con `alias(libs.plugins.marketjoya.android.feature)`.
- [ ] Paquete raíz `pe.marketjoya.feature.<x>`; capas solo `domain/application/infrastructure/presentation/components/navigation/di`.
- [ ] Un UseCase por paquete en minúsculas (`application/completesale/CompleteSaleUseCase.kt`).
- [ ] Domain/UseCase antes de Compose; puerto de escritura o lectura, no CRUD genérico.
- [ ] `:core:network`, `:core:database` o `:hardware` añadidos solo si el slice los usa.
- [ ] Adapter HTTP traduce errores con `HttpErrorMapper`; `ClientMutationId` si sincroniza.
- [ ] Si offline: `LocalId` + `PendingSyncWriter`; no borrar hasta ACK.
- [ ] ViewModel solo UseCases; Screen solo state + callbacks; tokens / `Mr*` del design-system.
- [ ] Módulo Koin en `di/` registrado en `di/PosModules.kt` y/o `di/PreventaModules.kt`.
- [ ] Tests del slice espejados en `src/test` / `src/androidTest` (`../../mobile-testing/SKILL.md`).
- [ ] Decisiones abiertas reportadas como "Pendiente de decisión".
