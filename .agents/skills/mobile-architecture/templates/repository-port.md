# Plantilla: puerto de escritura

Ruta: `feature/sales/src/main/kotlin/pe/marketjoya/feature/sales/domain/SaleWriter.kt`

```kotlin
package pe.marketjoya.feature.sales.domain

interface SaleWriter {
    suspend fun getById(id: SaleId): Sale?
    suspend fun add(sale: Sale)
}
```

- Implementación Retrofit/Room en `infrastructure/`. Techo corto; listados van a un puerto de lectura o a un GET del API.
- La idempotencia de sync no se repite aquí: usa `PendingSyncWriter.existsByMutationId(ClientMutationId)` de `:domain`.
- No repositorio genérico ni `getAll()`.
