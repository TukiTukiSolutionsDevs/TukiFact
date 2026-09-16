---
name: backend-testing
description: "Trigger: backend test, test .NET, xUnit, Testcontainers, integración backend, E2E HTTP backend, fixture, builder. Pruebas del backend Market Real por capa."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "2.5"
---

# Testing Backend — Market Real

## Activation Contract

Complementa a `backend-architecture` (estructura del código); aquí se define qué probar por capa y cómo, en `MarketjoyaBackend/tests/`. Activa al escribir, revisar o refactorizar pruebas backend (unitarias, integración, E2E, architecture tests, fixtures, builders, consumers, outbox/inbox) o al decidir si un caso de uso está listo.

## Hard Rules

- Cobertura por riesgo de negocio, no por porcentaje: dinero, stock y caja primero.
- Stack: Microsoft.Testing.Platform + xUnit v3 + FluentAssertions 7.x (8+ es comercial: no subir) + Testcontainers.
- Infra real; NSubstitute solo para SUNAT, OCR de IA y GPS.
- Dominio unitario y exhaustivo; casos de uso integrados vía `ISender`, nunca con repositorio mockeado.
- Tests espejan el vertical slice; sin carpetas horizontales.
- Mismas convenciones que producción: AAA, un comportamiento por test.
- Prohibido `Thread.Sleep`, orden entre tests y lógica condicional en el test.
- Un contenedor y una migración por colección xUnit; nada de crear, migrar o limpiar DB por `[Fact]`. Aislamiento por datos únicos.
- No se prueba el framework, Common ya cubierto, ni mappers/validadores triviales.
- Errores por `Error.Code` + `Error.Type` (validación: `FieldErrors`), nunca por descripción; en HTTP, §7 de `../backend-architecture/references/api-error-contract.md`.

## Decision Gates

| Cambiaste | Capa | Cómo | Proyecto `Marketjoya.*` |
|---|---|---|---|
| Agregado, VO, regla, evento de dominio | Domain | Unitario, sin IO | `Modules.<M>.UnitTests` |
| Command, query, handler de dominio | Application | Integración vía `ISender` + Postgres | `Modules.<M>.IntegrationTests` |
| Repositorio, EF config, read connection | Infrastructure | Integración de persistencia | `Modules.<M>.IntegrationTests` |
| Consumer, outbox/inbox, retry/DLQ | Infrastructure | Integración RabbitMQ/Mongo | `Modules.<M>.IntegrationTests` |
| Endpoint, auth, ProblemDetails | Presentation | E2E si es flujo crítico | `Api.E2ETests` |
| Mediador, behaviors, Result, interceptores | Common | Una vez | `Common.*Tests` |
| Frontera de capa o módulo | — | NetArchTest | `ArchitectureTests` |

## Execution Steps

1. Identifica capa y riesgo de negocio.
2. Lee `references/strategy.md` y la reference de la capa.
3. Elige proyecto según la tabla y espeja la carpeta del caso de uso.
4. Copia el template de `templates/` y usa builders.
5. Ejecuta el checklist de la capa en `checklists/`.
6. Corre el proyecto estrecho; architecture tests si cambió una frontera.
7. Flujo crítico: cubre también E2E (`references/presentation.md`).

## Output Contract

- Capa, proyecto y archivos.
- Comportamientos cubiertos (éxito, fallo de negocio, idempotencia si aplica).
- Qué se mockeó (solo externos) o "ningún mock".
- Checklist y comando de test corrido.
- Huecos de riesgo no cubiertos y confirmación de fixture de colección (DB no se recrea por test).

## References

- [references/strategy.md](references/strategy.md), [references/conventions.md](references/conventions.md), [references/architecture-tests.md](references/architecture-tests.md)
- Capas: [domain](references/domain.md), [application](references/application.md), [infrastructure](references/infrastructure.md), [presentation](references/presentation.md), [common](references/common.md)
- [templates/](templates/), [checklists/](checklists/)
- Docs: `MarketjoyaBackend/docs/testing.md`
- [../backend-architecture/SKILL.md](../backend-architecture/SKILL.md), [../frontend-testing/SKILL.md](../frontend-testing/SKILL.md), [../mobile-testing/SKILL.md](../mobile-testing/SKILL.md), [../test-e2e/SKILL.md](../test-e2e/SKILL.md)
