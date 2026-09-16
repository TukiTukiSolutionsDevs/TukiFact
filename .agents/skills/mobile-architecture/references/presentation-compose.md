# Presentation y Compose

```text
Route -> Screen(state, callbacks) -> ViewModel -> UseCases
```

- Route conoce ViewModel (`koinViewModel()`) y navegación.
- Screen es declarativa: state + eventos. No IO.
- ViewModel coordina UseCases. `UiState` durable, `UiEvent` intención, `UiEffect` puntual.
- Composable reusable no recibe ViewModel.
- `collectAsStateWithLifecycle` cuando corresponda.
- `LaunchedEffect` solo para efecto suspend de lifecycle; `DisposableEffect` si hay cleanup.
- Selectores de test: `testTag` estable; la raíz de la app expone `testTagsAsResourceId = true` (Maestro).

## Errores en UI

Contrato §6: `../../backend-architecture/references/api-error-contract.md`.

- `UiState` lleva el `AppError` recibido del UseCase (ej. `SalesUiState.Error(val error: AppError)`); no un `String` ni un `Throwable`.
- Ramifica por `type` primero y por `code` después. Nunca por `description` ni por status HTTP.
- Mensaje: texto local por `code` si existe; si no, `description` para errores 4xx; genérico para `Failure`, `Network` y `Unknown`, mostrando `correlationId` como referencia de soporte.
- `fieldErrors` se pintan en el campo correspondiente (`field` = ruta JSON camelCase del request); nunca solo con color.
- `Forbidden`: acción no permitida, sin reintento.
- `Unauthorized`: refresh de sesión single-flight una vez; si falla, cierre de sesión y navegación a login. Contrato de auth y dueño del refresh: Pendiente de decisión.
- Reintento manual solo para `Network`/`Failure` en operaciones idempotentes; `Concurrency.Conflict` recarga antes de reintentar.

| `type` | UI |
|---|---|
| `Validation` | Errores en campos + resumen |
| `Conflict` | Mensaje por `code` (`BusinessRule.<Regla>`, `Concurrency.Conflict` → recargar) |
| `NotFound` / `Forbidden` | Mensaje por `code`; sin reintento |
| `Failure` / `Network` / `Unknown` | Genérico + `correlationId`; reintento si aplica |
| `Unauthorized` | Refresh → logout |

## Estado actual

Las apps muestran un shell de arranque (`PosAppRoot` / `PreventaAppRoot`) dentro de `MrTheme`. No hay grafo de navegación ni features todavía.

## Design System

Antes de pintar: `../design-system/SKILL.md`. `:core:designsystem` hoy expone tokens generados (`generated/Mr*Tokens.kt`) y `MrTheme` (`MrTheme.colors`, `MrTheme.typography`); Material 3 queda interno al renderer. Componentes Compose `Mr*` (`MrButton`, …): se usan cuando existan en `:core:designsystem`; no se crean dentro de una feature.

Konsist `VisualLiterals` bloquea `Color(0x…)` y `<n>.dp`/`<n>.sp` fuera de los tokens generados. Los tokens generados no se editan a mano (`generateDesignTokens`).

POS: targets grandes, una acción primaria por sección, dinero con tabular nums (`design-system/rules/pos-rules.json`).

## Prohibido

Red, Room, Bluetooth o USB desde Compose. Lógica de negocio o parseo de hardware en Screen/ViewModel.

Streams de balanza/scanner: filtrar en el adapter; no recomponer por cada byte.
