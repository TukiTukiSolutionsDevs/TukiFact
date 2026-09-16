# Architecture tests

No sustituyen unitarias ni integración. Blindan fronteras para que el merge no meta un `DbContext` en un handler.

## Dónde

`MarketjoyaBackend/tests/Marketjoya.ArchitectureTests/` — corre en `dotnet test` y en CI; bloquea el merge.

```text
Solution/       ProductionSolution descubre src/**/*.csproj (ProjectReference por glob); SolutionDiscoveryTests
Dependencies/   LayerDependencyTests (tipos), ProjectReferenceTests (csproj y ensamblado de Contracts)
Conventions/    ApplicationConventionTests, CompositionConventionTests, MessagingConventionTests,
                PersistenceConventionTests, StructureConventionTests
Rules/          implementación de cada regla (NetArchTest + reflexión)
Fixtures/       violaciones deliberadas: módulo Billing (rompe reglas) y Shipping (limpio)
SelfCheck/      cada regla debe reportar su violación en Fixtures y no marcar los tipos válidos
```

Los módulos se descubren por nombre de proyecto (`Marketjoya.Modules.<M>.<Capa>`): un módulo nuevo queda cubierto sin editar los tests.

## Reglas (resumen)

- Solución: nombres y carpetas de `src/`, todo proyecto en la solución y cargado, cuatro capas por módulo.
- Dependencias: grafo de proyectos, paquetes prohibidos por capa, Contracts solo BCL, Domain solo BCL + `Common.Domain`, Application sin infraestructura, Infrastructure sin Presentation, Presentation sin datos, módulos sin referencias cruzadas.
- Application: handlers sin EF Core/`IQueryable`/Npgsql/Infrastructure, `internal sealed`, junto a su request; requests `public sealed` `*Command`/`*Query` (o `ICommand`); validators `internal sealed <Mensaje>Validator`; query handlers sin repositorios, EF Core ni `ITransactionManager`.
- Estructura: sin `Handlers/`, `Services/`, `Managers/`, `Helpers/`, `Dtos/`, `Repositories/`; `Shared/` solo dentro de un submódulo.
- Persistencia: repositorios ≤ 5 métodos sin `IQueryable`; sin repositorio genérico ni `IUnitOfWork`; entidades sin nombres reservados de shadow properties; `<Modulo>DbContext` en `Infrastructure/Persistence`.
- Mensajería: consumers `public sealed *Consumer` en `Infrastructure/Messaging`; eventos de integración solo en Contracts con sufijo `IntegrationEvent`; eventos de dominio en Domain con sufijo `DomainEvent`.
- Composición: un `<Modulo>Module : IModule` en la raíz de Infrastructure; endpoints `sealed <CasoDeUso>Endpoint` en `Presentation/<Submodulo>/<CasoDeUso>/`.

Tabla completa: `MarketjoyaBackend/docs/testing.md` (Architecture rules). Reglas de negocio de la arquitectura: `../backend-architecture/references/dependency-rules.md`.

Fuera de estos tests: rutas bajo `/api/v1` (grupo de `MapEndpoints`), nombres de endpoint (generación OpenAPI en build) y SQL entre schemas (revisión).

## Qué añadir cuando

| Cambio | ¿Nueva regla? |
|---|---|
| Módulo nuevo | No: se descubre solo; verificar que el proyecto está en la solución y sigue el esquema de nombres |
| Nueva frontera (p. ej. “queries no referencian EF”) | Sí: regla en `Rules/`, `[Theory]` en `Dependencies/` o `Conventions/`, violación en `Fixtures/Modules` y test en `SelfCheck/` |
| Excepción puntual | No se apaga el test; se corrige el código o se documenta la excepción en la regla |

## Cómo

NetArchTest + reflexión sobre ensamblados y lectura de `.csproj`. Nombre de test = comportamiento (`Handlers_AreSealedAndInternal`). No uses architecture tests para reglas de negocio.

Checklist: `checklists/architecture.md`.
