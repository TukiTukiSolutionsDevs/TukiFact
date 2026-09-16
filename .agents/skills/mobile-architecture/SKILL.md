---
name: mobile-architecture
description: "Trigger: Android, mobile, POS, preventa, pantalla Compose Android, Room, offline, sync. Convención Android de Market Real acoplada al backend."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "1.4"
---

# Arquitectura Mobile — Market Real

## Activation Contract

Android nativo en `MarketjoyaMobile/` (`:app-pos`, `:app-preventa` + módulos compartidos). El API `.NET` es la autoridad. Activa al crear, revisar o explicar módulos Gradle, features, UseCases, Room, Retrofit, Koin, Compose, sync/outbox, hardware, POS o preventa.

## Hard Rules

- `:domain`/`:hardware` son Kotlin/JVM; `domain/` y `application/` de feature no importan `android.`, `androidx.`, `retrofit2.`, `okhttp3.`.
- UseCase sin `Context`, navegación, Room ni DAO. ViewModel sin DAO, USB, Bluetooth, Room ni HTTP. Composable sin infraestructura.
- Feature = `:feature:<x>` con capas `domain/application/infrastructure/presentation/components/navigation/di`; un UseCase por paquete en minúsculas (`application/completesale/`). Paquete = namespace del módulo.
- DTO ≠ Entity Room ≠ Domain ≠ UiModel; mappers explícitos.
- Mutación = Command; lectura = Query bajo `/api/v1/...`. No inventes endpoints.
- Sync: `LocalId` + `ClientMutationId` vía `PendingSyncWriter`; outbox no se borra antes del ACK. Fiscal solo en backend.
- Visual solo tokens generados / `Mr*`; sin HEX ni `dp`/`sp` literales.
- Error: UseCase retorna `AppResult<T>` con `AppError` (`type` + `code`); sin `kotlin.Result` ni excepciones. Solo `HttpErrorMapper` lee ProblemDetails.
- Sin `GlobalScope`, secretos ni UI en singleton. DI solo Koin.
- Lo no decidido se reporta como "Pendiente de decisión".

## Decision Gates

| Situación | Acción |
|---|---|
| Nueva feature | `checklists/new-feature.md` |
| Mutación | UseCase + puerto de escritura; `templates/use-case.md` |
| Lectura | Puerto de lectura; Response del API |
| Offline / sync | `references/data-and-offline.md` |
| Error | Contrato canónico (References); `references/domain-and-use-cases.md` |
| Hardware | `references/hardware.md` |
| UI | `../design-system/SKILL.md` |
| Contrato API | `references/backend-coupling.md` |

## Execution Steps

1. Identifica app, módulo y tipo: comando, query, hardware o sync.
2. Lee `references/architecture-map.md` y `references/backend-coupling.md`.
3. Confirma el Command/Query en `../backend-architecture/SKILL.md`.
4. Dominio y UseCase antes de Compose; parte de `templates/`.
5. Respeta `references/dependency-rules.md`; revisa con `checklists/architecture-review.md`.
6. Tests con `../mobile-testing/SKILL.md`; cierra con `checklists/verification.md`.

## Output Contract

- Módulos y capas tocados.
- Command/Query, o "solo local / hardware".
- Puertos y fakes; sync o "online-only".
- Comandos ejecutados o no, con motivo.
- Bloqueadores primero; decisiones abiertas como "Pendiente de decisión".

## References

- [references/architecture-map.md](references/architecture-map.md) · [references/backend-coupling.md](references/backend-coupling.md) · [references/dependency-rules.md](references/dependency-rules.md)
- [references/domain-and-use-cases.md](references/domain-and-use-cases.md) · [references/data-and-offline.md](references/data-and-offline.md) · [references/presentation-compose.md](references/presentation-compose.md)
- [references/hardware.md](references/hardware.md) · [references/pos-domain.md](references/pos-domain.md) · [references/presales-domain.md](references/presales-domain.md)
- [references/patterns.md](references/patterns.md) · [references/security.md](references/security.md) · [references/testing.md](references/testing.md)
- [templates/](templates/) · [checklists/](checklists/)
- [../mobile-testing/SKILL.md](../mobile-testing/SKILL.md) · [../backend-architecture/SKILL.md](../backend-architecture/SKILL.md) · [../design-system/SKILL.md](../design-system/SKILL.md)
- [../backend-architecture/references/api-error-contract.md](../backend-architecture/references/api-error-contract.md)
