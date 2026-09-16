# Plantilla: UseCase JVM

Ruta: `feature/sales/src/test/kotlin/pe/marketjoya/feature/sales/application/completesale/CompleteSaleUseCaseTest.kt`

```kotlin
package pe.marketjoya.feature.sales.application.completesale

import pe.marketjoya.core.testing.sync.FakePendingSyncWriter
import pe.marketjoya.domain.error.AppResult
import pe.marketjoya.domain.error.ErrorType
import pe.marketjoya.domain.sync.ClientMutationId
import pe.marketjoya.domain.sync.LocalId

class CompleteSaleUseCaseTest {
    private val sales = FakeSaleWriter()
    private val outbox = FakePendingSyncWriter()
    private val completeSale = CompleteSaleUseCase(sales, outbox)

    @Test
    fun invoke_validSale_enqueuesOutbox() = runTest {
        val sale = SaleBuilder().openShift().withItem().build()
        sales.add(sale)
        val mutation = ClientMutationId.random()

        val result = completeSale(CompleteSale(sale.id, LocalId.random(), mutation))

        assertEquals(AppResult.Success(sale.id), result)
        assertEquals(1, outbox.pendingFor(mutation).size)
    }

    @Test
    fun invoke_unknownSale_returnsNotFoundAndDoesNotEnqueue() = runTest {
        val mutation = ClientMutationId.random()

        val result = completeSale(CompleteSale(SaleId.random(), LocalId.random(), mutation))

        val failure = result as AppResult.Failure
        assertEquals(ErrorType.NotFound, failure.error.type)
        assertEquals("Sale.NotFound", failure.error.code)
        assertFalse(outbox.existsByMutationId(mutation))
    }
}
```

- `FakeSaleWriter`, `SaleBuilder`, `CompleteSale`, `SaleId` son ilustrativos; `FakePendingSyncWriter`, `LocalId`, `ClientMutationId`, `AppResult` y `ErrorType` (8 valores) existen.
- Aserta `type` + `code`; nunca `description`. Sin `kotlin.Result` ni `assertThrows` para reglas predecibles.
- Idempotencia del outbox ya la cubre `PendingSyncWriterContract`; aquí se prueba la regla del UseCase.
