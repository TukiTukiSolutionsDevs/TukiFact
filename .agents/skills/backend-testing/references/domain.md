# Capa: Domain

Qué vive aquí: agregados, value objects, reglas (`IBusinessRule`), specifications, eventos de dominio, domain services, `*Errors`. Cero IO.

## Qué probar

| Pieza | Comportamiento a demostrar |
|---|---|
| Agregado | Factory/`Create`, transiciones (reserva, pago, cierre), invariantes rotas |
| Value object | `Create` válido e inválido; igualdad por componentes |
| `IBusinessRule` | `IsBroken` true/false con datos de borde |
| Specification | `IsSatisfiedBy` en combinaciones reales (crédito, producto vendible) |
| Domain service | Regla que cruza agregados (email único, tope de crédito con deuda) |
| Evento de dominio | Se emite al completar el hecho; no se emite si el resultado es failure |

Prioridad: `StockItem`, `Sale`, `CreditLine`, `CashClosing`, motor multiempresa, granel.

## Qué no probar

- Persistencia, SQL, HTTP, serializers.
- Handlers, endpoints, DbContext.
- Getters triviales o igualdad heredada de `Entity`/`ValueObject` sin lógica propia.

## Dónde

```text
tests/Marketjoya.Modules.<X>.UnitTests/
└── <Submodulo>/
    ├── <Agregado>Tests.cs
    ├── <Vo>Tests.cs
    ├── Rules/
    ├── Specifications/
    └── Builders/
        └── <Agregado>Builder.cs
```

Un archivo por tipo bajo prueba. Namespace = ruta. Sin `WebApplicationFactory`, Testcontainers ni NSubstitute.

## Cómo

1. Builder + Bogus (`templates/domain-aggregate.md`, `templates/test-data-builder.md`).
2. Actúa sobre el agregado en memoria: `item.Reserve(...)`, `Sale.Create(...)`.
3. Assert de `Result` / `BusinessRuleValidationException`, estado y eventos (`GetDomainEvents()`).
4. Variaciones de la misma regla → `[Theory]` con datos nombrados por escenario.
5. `IClock` se pasa como valor (`DateTimeOffset`) o fake mínimo si el agregado lo recibe; no mockees repositorios aquí. Si la regla necesita un checker (email único), un fake in-memory de esa interfaz de dominio basta.

## Escenarios mínimos por agregado nuevo

- [ ] Happy path de creación y una transición principal.
- [ ] Cada invariante rota (stock insuficiente, tope de crédito, doble llave ausente).
- [ ] Evento de dominio emitido solo en éxito.
- [ ] IDs/valores de borde (0, negativo, vacío) si el lenguaje del dominio los rechaza.

Checklist: `checklists/domain.md`.
