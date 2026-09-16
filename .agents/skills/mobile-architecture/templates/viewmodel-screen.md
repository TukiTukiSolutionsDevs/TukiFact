# Plantilla: Route / Screen / ViewModel

Ruta: `feature/sales/src/main/kotlin/pe/marketjoya/feature/sales/presentation/`

```kotlin
package pe.marketjoya.feature.sales.presentation

import pe.marketjoya.domain.error.AppError
import pe.marketjoya.domain.error.AppResult
import pe.marketjoya.domain.error.ErrorType

sealed interface SalesUiState {
    data object Idle : SalesUiState
    data object Loading : SalesUiState
    data class Success(val saleId: SaleId) : SalesUiState
    data class Error(val error: AppError) : SalesUiState
}

@Composable
fun SalesRoute(vm: SalesViewModel = koinViewModel()) {
    val state by vm.state.collectAsStateWithLifecycle()
    SalesScreen(state = state, onConfirm = vm::onConfirm)
}

@Composable
fun SalesScreen(
    state: SalesUiState,
    onConfirm: () -> Unit,
    modifier: Modifier = Modifier,
) {
    // Componente Mr* de :core:designsystem (MrButton cuando exista) + MrTheme.colors / MrTheme.typography.
    // testTag estable para Compose UI test y Maestro, ej. Modifier.testTag("confirm-sale").
    // Error: fieldErrors junto a su campo; Failure/Network/Unknown -> mensaje genérico + correlationId.
}

class SalesViewModel(
    private val completeSale: CompleteSaleUseCase,
) : ViewModel() {
    private val _state = MutableStateFlow<SalesUiState>(SalesUiState.Idle)
    val state: StateFlow<SalesUiState> = _state.asStateFlow()

    fun onConfirm() {
        viewModelScope.launch {
            _state.value = SalesUiState.Loading
            _state.value = when (val result = completeSale(command())) {
                is AppResult.Success -> SalesUiState.Success(result.value)
                is AppResult.Failure -> SalesUiState.Error(result.error)
                // Unauthorized: refresh una vez -> logout (contrato de auth: Pendiente de decisión).
            }
        }
    }
}

// Texto al usuario: ramifica por type primero y por code después; nunca por description.
fun errorText(error: AppError): UiText = when (error.type) {
    ErrorType.Failure, ErrorType.Network, ErrorType.Unknown ->
        UiText.Generic(supportReference = error.correlationId)
    ErrorType.Conflict -> when (error.code) {
        "Concurrency.Conflict" -> UiText.Res(R.string.sale_reload_required) // recargar antes de reintentar
        "Sale.ShiftClosed" -> UiText.Res(R.string.sale_shift_closed)
        else -> UiText.Plain(error.description)
    }
    else -> UiText.Plain(error.description) // 4xx sin texto local
}
```

- `SalesUiState`, `SaleId`, `UiText`, los recursos `R.string.*` y `command()` son ilustrativos. Sin DAO, USB ni HTTP en el ViewModel.
- El ViewModel no traduce status HTTP ni lee JSON: recibe `AppError` ya mapeado por `HttpErrorMapper`.
- Texto al usuario: local por `code` si existe; si no, `description` en 4xx; genérico + `correlationId` en `Failure`/`Network`/`Unknown` (`references/presentation-compose.md`).

Registro en `feature/sales/.../di/`: `viewModelOf(::SalesViewModel)` o `viewModel { SalesViewModel(get()) }` (Koin). La Screen no conoce Retrofit.
