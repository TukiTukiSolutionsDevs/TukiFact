# Plantilla: ViewModel + Turbine

Ruta: `feature/sales/src/test/kotlin/pe/marketjoya/feature/sales/presentation/SalesViewModelTest.kt`

```kotlin
package pe.marketjoya.feature.sales.presentation

import pe.marketjoya.core.testing.MainDispatcherRule
import pe.marketjoya.domain.error.AppError
import pe.marketjoya.domain.error.AppResult
import pe.marketjoya.domain.error.ErrorType

class SalesViewModelTest {
    @get:Rule val main = MainDispatcherRule()

    @Test
    fun onConfirm_success_emitsSuccess() = runTest {
        val saleId = SaleId.random()
        val vm = SalesViewModel(completeSale = FakeCompleteSale(outcome = AppResult.Success(saleId)))
        vm.state.test {
            skipItems(1) // idle
            vm.onConfirm()
            assertEquals(SalesUiState.Loading, awaitItem())
            assertEquals(SalesUiState.Success(saleId), awaitItem())
        }
    }

    @Test
    fun onConfirm_shiftClosed_emitsErrorWithAppError() = runTest {
        val error = AppError(ErrorType.Conflict, "Sale.ShiftClosed", "No hay turno de caja abierto.")
        val vm = SalesViewModel(completeSale = FakeCompleteSale(outcome = AppResult.Failure(error)))
        vm.state.test {
            skipItems(1) // idle
            vm.onConfirm()
            assertEquals(SalesUiState.Loading, awaitItem())
            val state = awaitItem() as SalesUiState.Error
            assertEquals(ErrorType.Conflict, state.error.type)
            assertEquals("Sale.ShiftClosed", state.error.code)
        }
    }
}
```

- `SalesViewModel`, `SalesUiState`, `SaleId` y `FakeCompleteSale` son ilustrativos.
- El fake devuelve `AppResult`; no `kotlin.Result.success/failure`.
- Aserta `type` + `code` del `AppError` en el `UiState`; nunca `description`.
- Casos adicionales según la pantalla: `Validation` con `fieldErrors`, `Network`/`Failure` con `correlationId` visible, `Unauthorized` → refresh/logout (cuando exista el contrato de auth).
