# Plantilla: Compose

Ruta: `feature/sales/src/androidTest/kotlin/pe/marketjoya/feature/sales/presentation/SalesScreenTest.kt`

```kotlin
package pe.marketjoya.feature.sales.presentation

class SalesScreenTest {
    @get:Rule val compose = createComposeRule()

    @Test
    fun confirm_whenCanConfirm_isEnabled() {
        compose.setContent {
            MrTheme {
                SalesScreen(state = SalesUiState(canConfirm = true), onConfirm = {})
            }
        }
        compose.onNodeWithTag("confirm-sale").assertIsEnabled()
    }
}
```

`SalesScreen` y `SalesUiState` son ilustrativos; `MrTheme` existe en `:core:designsystem`.
