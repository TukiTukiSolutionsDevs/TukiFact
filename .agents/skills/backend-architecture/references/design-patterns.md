# Patrones de diseño — cuándo usarlos y cuándo no

## Principio rector

Un patrón entra cuando resuelve una complejidad real y presente, no una imaginaria. Criterio mínimo: existen 2–3 variaciones concretas (o se sabe con certeza contractual que vendrán) y el patrón reduce el código o el riesgo de cambio. Si solo hay una variante, el patrón es ruido. El código simple primero; el patrón emerge por refactor cuando la segunda variante aparece.

Ningún patrón se aplica "en todas partes". Cada uno tiene su sitio exacto en esta arquitectura.

## Decorator

**Qué resuelve:** lógica transversal (validación, transacción, logging, telemetría, caché) sin ensuciar handlers.

**Dónde:**

- Los pipeline behaviors del mediador propio ya son Decorator (`IPipelineBehavior<TReq, TRes>`). Es la forma preferida: no crear decoradores manuales para lo que un behavior resuelve.
- Decorador manual solo cuando aplica a un puerto específico: p. ej. `CachedProductReadConnection : IProductsReadDbConnection`.

```csharp
services.AddScoped<ProductsReadDbConnection>();
services.AddScoped<IProductsReadDbConnection>(sp =>
    new CachedProductsReadDbConnection(
        sp.GetRequiredService<ProductsReadDbConnection>(),
        sp.GetRequiredService<IMemoryCache>()));
```

**Cuándo no:** pasos de un caso de uso individual (eso es el handler); cuando un behavior ya lo cubre; 4+ niveles anidados.

**Dónde vive:** junto a la implementación que envuelve, en Infrastructure (`Infrastructure/Users/CachedUsersReadDbConnection.cs`). Behaviors globales en `Common.Application/Behaviors/`.

## Strategy

**Qué resuelve:** algoritmos intercambiables seleccionados por contexto o configuración.

**Usos reales:**

- Motor multiempresa: `ICompanyDeterminationStrategy` por regla (`emission_rules`).
- Precio por lista: `IPricingStrategy` (minorista, mayorista, distribución, granel).
- Granel: estrategia por tipo de pesaje.

```csharp
public interface ICompanyDeterminationStrategy
{
    string RuleKey { get; }
    CompanyId Determine(SaleContext context);
}
```

**Cuándo no:** una sola variante → un método. Dos variantes estables → un `if` honesto. Strategy paga con 3+ variantes o reglas que configura el cliente.

**Dónde vive:** interfaz en Domain (si es regla de negocio) o `Application/Abstractions/`; implementaciones en Domain (puras) o Infrastructure. El selector es Factory.

## Factory

**Qué resuelve:** creación compleja o selección entre variantes.

**Usos reales:**

- Selector de estrategias: `ICompanyDeterminationStrategyFactory.Resolve(ruleKey)`.
- `User.Create(...)` es factory method — suficiente en la mayoría. Clase Factory aparte solo con dependencias externas (`ElectronicDocumentFactory` en Infrastructure de Billing).
- `ISqlConnectionFactory` en Common.Application.

**Cuándo no:** `new X()` simple; factories genéricas `IFactory<T>`; "por si mañana hay más tipos".

**Dónde vive:** factory methods estáticos en el agregado; clases factory en Infrastructure si necesitan dependencias; selector de strategy junto a las estrategias.

## Builder

**Qué resuelve:** construcción paso a paso de objetos complejos con partes opcionales.

**Usos reales:**

- Documentos electrónicos SUNAT: `ElectronicDocumentBuilder`.
- Tests: `SaleBuilder.ForRegister(x).WithItem(...).PaidWith(...)` cuando el agregado tiene muchas invariantes.

```csharp
var document = new ElectronicDocumentBuilder(company, series)
    .WithCustomer(customer)
    .AddItem(item1).AddItem(item2)
    .WithRelatedGuide(guideId)
    .Build();
```

**Cuándo no:** objetos de 3–5 campos → constructor o factory method. Nunca builders para Commands/Queries/DTOs.

**Dónde vive:** junto al agregado/documento (Domain si es puro, Infrastructure si usa dependencias); test builders en el proyecto de tests del módulo.

## Specification

**Qué resuelve:** predicados de negocio reutilizables y combinables con nombre del lenguaje ubicuo.

**Usos reales:**

- Elegibilidad de crédito: `CustomerIsEligibleForCreditSpecification` (Orders, Sales, Customers).
- Producto vendible en caja: `new ProductIsActiveSpec().And(new BelongsToWarehouseSpec(Deposito1))`.

```csharp
public sealed class CustomerIsEligibleForCreditSpecification : ISpecification<Customer>
{
    public bool IsSatisfiedBy(Customer c) =>
        c.CreditLine.IsActive && c.CreditLine.CurrentDebt < c.CreditLine.Limit
        && !c.CreditLine.HasExpiredDebt(clock.UtcNow);
}
```

**Cuándo no:** NUNCA como capa sobre el repositorio (`repo.Find(spec)` con `Expression<Func<T,bool>>`). Eso resucita el repositorio genérico y genera SQL impredecible. Las specifications evalúan objetos de dominio en memoria. Consultas a base de datos: query side con SQL explícito.

**Dónde vive:** Domain, junto al agregado (`Domain/Users/Specifications/...` o carpeta del submódulo).

## Visitor

**Qué resuelve:** agregar una operación sobre una jerarquía de tipos estable sin modificar los tipos.

**Uso real (el único por ahora):** movimientos de kardex (`StockMovement`: compra, venta, traslado, ajuste, retorno apto, merma). El catálogo de tipos es estable; las operaciones crecen (reporte, valorización, asiento). `IStockMovementVisitor`.

**Cuándo no:** la jerarquía cambia seguido. Pocos subtipos y pocas operaciones → `switch` expression. Visitor solo cuando las operaciones crecen más rápido que los tipos.

**Dónde vive:** Domain junto a la jerarquía (`Domain/Inventory/Movements/IStockMovementVisitor.cs`).

## Singleton

**Regla:** el Singleton lo gestiona el contenedor DI (`AddSingleton`), nunca la clase (`Instance` estático está prohibido: rompe tests y oculta dependencias).

**Usos reales:** `NpgsqlDataSource` e `ISqlConnectionFactory` (`NpgsqlSqlConnectionFactory`) sin estado; `TimeProvider` e `IClock`; `SyncVersionInterceptor`; `HttpClient` vía `IHttpClientFactory`; `IOptions<T>`. Previstos: `IMemoryCache`, caches de catálogo POS.

**Cuándo no:** estado mutable compartido sin lock; cualquier cosa que un test deba fakear (usar Scoped); jamás estado de negocio en un singleton.

## Tabla de decisión

| Situación | Patrón | Alternativa si es simple |
|---|---|---|
| Lógica transversal en todos los casos de uso | Decorator vía behavior del mediador | — (ya existe en Common) |
| Caché/retry en UN puerto | Decorator manual + DI | — |
| Algoritmo con 3+ variantes configurables | Strategy + Factory selector | `if` si son 2 estables |
| Creación con dependencias externas | Factory class | factory method estático en el agregado |
| Objeto complejo con partes opcionales | Builder | constructor directo |
| Predicado de negocio reutilizado en el mismo BC | Specification (en memoria) | método bool en el agregado |
| Operaciones nuevas sobre jerarquía estable | Visitor | `switch` expression |
| Instancia única | Singleton por DI | — |
| "¿Y si mañana...?" | NINGUNO | código simple, refactor después |
