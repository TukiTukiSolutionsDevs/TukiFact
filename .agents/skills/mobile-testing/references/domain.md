# Capa: Domain

JVM (`:domain` o `feature/<x>/domain/`). Sin Android. Builders + escenarios de borde.

## Qué

Invariantes (`Sale.complete`, stock no baja al agregar al carrito), VOs, specs, transiciones de estado. Existente: `PendingSyncOperationTest` (transiciones `acknowledge`/`markConflict`/`reject`, `rejection` presente solo en `Rejected`, `kind` vacío), `SyncAttemptPolicyTest` (decisión por `type` y `code`), `AppErrorTest`, `AppResultTest` y `SyncIdentifiersTest` (parseo UUID canónico).

## Cómo

JUnit 4 + `org.junit.Assert`. Nombre `complete_withoutShift_fails`. El builder produce un agregado válido; el test ejecuta la operación que rompe. Errores de negocio se asertan como `AppResult.Failure` con `type` y `code` del `<Aggregate>Errors` (ej. `SaleErrors.ShiftClosed`), no como excepción ni por `description`.

Alineado a `backend-testing` Domain: `StockItem`, `Sale`, `CreditLine`, `CashClosing` primero.

No Room. No `Application` Context.
