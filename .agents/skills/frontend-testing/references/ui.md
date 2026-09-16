# Capa: UI (service, store, guard)

## Service / store

Fake del token de entrada (el use case ya está probado en core). Demuestra:

- `loading` → `success` y store actualizado **solo** si `GoResult` es ok.
- `error` conserva el `AppError` (`type` + `code`); el toast/mensaje no traga `UNAUTHORIZED` como genérico.
- Mensaje: texto local por `code`; `description` en 4xx sin texto local; genérico + `correlationId` visible en `FAILURE`/`NETWORK`/`UNKNOWN`.
- `VALIDATION`: `fieldErrors` llegan al campo correspondiente.
- Misma `type` + `code` con otra `description` → misma decisión (no se ramifica por `description`).
- Operaciones concurrentes no comparten un único `loadState` si pueden solaparse.

Facade: un test de “delega y no interpreta error” si hay duda; si es una línea, no hace falta.

## Screen

Solo si hay lógica de template (permiso de botón, empty/error). Preferir `data-testid`. No TestBed de la app completa.

## Guards

Usuario anónimo, lectura sin escritura, permiso insuficiente. El guard no es la autorización del API.

## Signals

Crear `resource` / `linkedSignal` dentro de contexto de inyección (`TestBed.runInInjectionContext` si no es un componente). Flush de effects antes de asertar.
