# Dominio y casos de uso

## Modelado

| Tipo | Criterio |
|---|---|
| Entity | Identidad |
| Value Object | Valor inmutable (`Money`, `LocalId`) |
| Aggregate | Consistencia transaccional en memoria |
| Error | `AppError` declarado como miembro estático por agregado (`SaleErrors.NotFound`) |
| DTO | Transporte HTTP (`:core:network`, `infrastructure/`) |
| DB Entity | Room (`:core:database`, `infrastructure/`) |
| UI Model | Presentación |

No uses el DTO de Retrofit como dominio ni como UI.

## Errores (contrato)

Fuente canónica: `../../backend-architecture/references/api-error-contract.md`. Si esta referencia contradice el contrato, manda el contrato.

`:domain` `error/`:

```kotlin
sealed interface AppResult<out T> {
    data class Success<T>(val value: T) : AppResult<T>
    data class Failure(val error: AppError) : AppResult<Nothing>
}

data class AppError(
    val type: ErrorType,
    val code: String,
    val description: String,
    val fieldErrors: List<FieldError> = emptyList(),
    val status: Int? = null,
    val traceId: String? = null,
    val correlationId: String? = null,
    val retryAfterSeconds: Long? = null,
)

data class FieldError(val field: String, val code: String, val description: String)

enum class ErrorType { Validation, Unauthorized, Forbidden, NotFound, Conflict, Failure, Network, Unknown }
```

- Lista cerrada de 8 `ErrorType`; agregar uno es cambio de contrato en las tres plataformas.
- `code` con formato `<Ámbito>.<Motivo>` en PascalCase (`Sale.NotFound`, `CashRegister.AlreadyOpen`). Es API pública: no se renombra ni se reutiliza.
- Errores estáticos por agregado en `<Aggregate>Errors` (`SaleErrors.NotFound`), nunca inline en el UseCase.
- Reglas predecibles devuelven `AppResult.Failure`; no lanzan excepciones.
- No uses `kotlin.Result`: exige `Throwable`.
- `status`, `traceId`, `correlationId` y `retryAfterSeconds` solo los rellena el adapter HTTP; el dominio los deja en `null`.
- Ninguna lógica lee `description`.

```kotlin
object SaleErrors {
    val NotFound = AppError(ErrorType.NotFound, "Sale.NotFound", "La venta no existe.")
    val ShiftClosed = AppError(ErrorType.Conflict, "Sale.ShiftClosed", "No hay turno de caja abierto.")
}
```

## Estado de implementación

Implementado (contrato §8): `:domain` `error/AppError.kt` (`ErrorType` de 8 valores, `FieldError`, `AppError` con `retryAfterSeconds`) y `error/AppResult.kt`, probados en `AppErrorTest` y `AppResultTest`. La regla Konsist `KotlinResultReturn` rechaza funciones que devuelven `kotlin.Result` (`references/dependency-rules.md`).

## UseCases

Nombre de intención, alineado al Command/Query del API: `AddProductToSale`, `CompleteSale`, `OpenCashShift`.

```kotlin
// feature/sales/src/main/kotlin/pe/marketjoya/feature/sales/application/completesale/CompleteSaleUseCase.kt
package pe.marketjoya.feature.sales.application.completesale

class CompleteSaleUseCase(
    private val sales: SaleWriter,
    private val outbox: PendingSyncWriter,
) {
    suspend operator fun invoke(command: CompleteSale): AppResult<SaleId>
}
```

- Retorna `AppResult<T>`; la UI ramifica por `type` y luego por `code`.
- Un UseCase por paquete en minúsculas (`application/completesale/`), verificado por Konsist `UseCaseFolder`.
- No navega. No conoce `Context`, DAO, Room ni SDK.

Reglas de negocio POS/preventa: `references/pos-domain.md`, `references/presales-domain.md`.
