---
name: backend-architecture
description: "Trigger: backend .NET, command/query, módulo, endpoint, agregado, repositorio, Wolverine, Clean Architecture backend. Monolito modular .NET 10 de Market Real."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "2.4"
---

# Arquitectura Backend — Market Real

## Activation Contract

Estándar obligatorio de `MarketjoyaBackend/`: activa al escribir, refactorizar o revisar código backend (capas, casos de uso, endpoints, agregados, eventos, errores, persistencia, telemetría, OpenAPI).

## Hard Rules

- Un host `Marketjoya.Api`; cada módulo son 4 proyectos `Marketjoya.Modules.<Modulo>.<Capa>`. Common también va por capas.
- Mediador propio en `Common.Application/Messaging`; no MediatR. Wolverine solo para integración.
- EF Core escribe; Dapper lee. Un schema Postgres por módulo.
- `TransactionBehavior` abre la transacción vía `ITransactionManager`. Prohibidos `IUnitOfWork`, `SaveChanges` en handlers y `DbContext` fuera de Infrastructure.
- Flujo previsible con `Result`, no excepciones; errores según `references/api-error-contract.md` (`<Agregado>Errors`, `code` estable, 400 con `errors`).
- Vertical slice por submódulo; Application y Presentation con carpeta por caso de uso.
- Un módulo no referencia otro; solo `BuildingBlocks.Contracts`.
- Repositorio por agregado, solo escritura, máx. 5 métodos, sin `IQueryable` ni `IRepository<T>`.
- Endpoints bajo `/api/v1` con `WithName`; commitear `openapi/marketjoya-api-v1.json`.
- Lo no decidido se marca "Pendiente de decisión"; no se inventa.

## Decision Gates

| Situación | Acción |
|---|---|
| Mutación | Command + EF + `templates/command.md` |
| Lectura | Query + Dapper + `templates/query.md` |
| Endpoint HTTP | `templates/endpoint.md` |
| Error, `code`, ProblemDetails | `references/errors-and-results.md` |
| DTO en ≥2 casos del submódulo | `<Submodulo>/Shared/`; si no, carpeta del caso de uso |
| Reacción en el mismo módulo | Evento de dominio + `IDomainEventHandler<T>` |
| Otro módulo debe enterarse | Evento de integración + `IIntegrationEventPublisher` |
| Puerto escritura / lectura | Repositorio del agregado / `I<Modulo>ReadDbConnection` |
| Módulo nuevo | `templates/module.md` + `checklists/new-module.md` |
| ¿Patrón de diseño? | `references/design-patterns.md` |

## Execution Steps

1. Identifica módulo, submódulo y tipo: comando, query, módulo nuevo o revisión.
2. Lee una rebanada equivalente y `references/architecture-map.md`.
3. Aplica el checklist de `checklists/` y copia `templates/`.
4. Coloca archivos según el árbol vertical; namespace file-scoped = ruta.
5. Define errores estáticos y contrato HTTP (`references/errors-and-results.md`).
6. Clasifica eventos con `references/events.md`; cumple `checklists/telemetry.md`.
7. En revisión: `checklists/architecture-review.md` + `references/dependency-rules.md`.
8. Verifica con `checklists/verification.md`; con código nuevo carga `../backend-testing/SKILL.md`.

## Output Contract

- Archivos tocados por capa; comando o query y por qué.
- Puertos, eventos y códigos de error nuevos (o "ninguno").
- Checklist y comandos corridos, o motivo si no se corrieron.
- Primero los bloqueadores: dependencia cruzada, `DbContext` en handler, schema ajeno, `code` renombrado, correlation id perdido, OpenAPI sin commitear.

## References

- [architecture-map](references/architecture-map.md), [dependency-rules](references/dependency-rules.md), [common](references/common-shared-kernel.md), [conventions](references/conventions.md)
- [error-contract](references/api-error-contract.md), [errors](references/errors-and-results.md), [events](references/events.md), [patterns](references/design-patterns.md), [telemetry](references/telemetry.md)
- [templates](templates/), [checklists](checklists/)
- Docs: `MarketjoyaBackend/docs/configuration.md`, `MarketjoyaBackend/docs/migrations.md`, `MarketjoyaBackend/docs/openapi.md`
- [backend-testing](../backend-testing/SKILL.md), [hexaclean](../hexaclean-architecture/SKILL.md), [mobile](../mobile-architecture/SKILL.md)
