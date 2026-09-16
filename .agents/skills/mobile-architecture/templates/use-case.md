# Plantilla: UseCase

Ruta: `feature/sales/src/main/kotlin/pe/marketjoya/feature/sales/application/completesale/CompleteSaleUseCase.kt`

```kotlin
package pe.marketjoya.feature.sales.application.completesale

import pe.marketjoya.domain.error.AppResult
import pe.marketjoya.domain.sync.PendingSyncOperation
import pe.marketjoya.domain.sync.PendingSyncWriter

class CompleteSaleUseCase(
    private val sales: SaleWriter,
    private val outbox: PendingSyncWriter,
) {
    suspend operator fun invoke(command: CompleteSale): AppResult<SaleId> {
        val sale = sales.getById(command.saleId)
            ?: return AppResult.Failure(SaleErrors.NotFound)
        // Regla de dominio incumplida -> AppResult.Failure, sin excepción y sin encolar.
        val completed = when (val result = sale.complete(command)) {
            is AppResult.Failure -> return result
            is AppResult.Success -> result.value
        }
        sales.add(completed)
        outbox.enqueue(
            PendingSyncOperation(
                localId = command.localId,
                clientMutationId = command.clientMutationId,
                kind = "CompleteSale",
            ),
        )
        return AppResult.Success(completed.id)
    }
}
```

- `SaleWriter`, `Sale`, `SaleErrors`, `SaleId` y `CompleteSale` son ilustrativos: no existen aún.
- `PendingSyncOperation`, `PendingSyncWriter`, `LocalId`, `ClientMutationId`, `AppResult` y `AppError` existen en `:domain` (`references/domain-and-use-cases.md`).
- `SaleErrors.NotFound` es un `AppError` estático con `code = "Sale.NotFound"`; nunca inline.
- Sin `kotlin.Result`, sin `throw` para reglas predecibles. `kind` y payload corresponden al Command del API. Sin `Context` ni DAO.
