# Convenciones frontend

## Nombre

`metodo_escenario_resultado`. Ejemplos del repo: `goOk_withData_returnsDataAndNullError`, `execute_concurrentCalls_refreshesOnce`, `appShell_open_rendersShell`.

Con `it.each`, placeholders en el nombre: `'toAppError_httpStatus%i_returns%s'`.

## AAA

`// Arrange` `// Act` `// Assert` (omite `Arrange` si no hay preparación). Un `it` = un comportamiento. `it.each` para datos.

## Imports

- `src/**/*.spec.ts`: globals de Vitest (`describe`, `it`, `expect`) sin import; aliases `@base/*`, `@core/*`…; el archivo bajo prueba con import relativo (`./refresh-session.use-case`).
- `tools/**/*.spec.mts` y `e2e/**`: imports explícitos (`vitest`, `@playwright/test`).

## Fakes

Clase in-memory que implementa el puerto y registra llamadas (`calls`). Ejemplos: `DeferredSessionRefresher` (promesas controladas para concurrencia), `ScriptedOperation` (resultado por llamada, falla ante una llamada inesperada). `vi.fn()` solo si el retorno no importa. Prohibido mock que siempre resuelve `undefined`.

## Aislamiento

Ids/`data-testid`/URLs únicos por caso. No asertas "hay 1 fila en la tabla" si el store es compartido.

## Prohibido

`sleep` / `waitForTimeout`. Tests que dependen del orden de archivos. Importar adapter desde un spec de core. HEX o clases Tailwind de marca en el spec como "API visual".
