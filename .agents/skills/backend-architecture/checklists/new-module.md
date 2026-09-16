# Checklist: módulo nuevo

- [ ] Cuatro proyectos `src/Modules/<Modulo>/Marketjoya.Modules.<Modulo>.{Domain,Application,Infrastructure,Presentation}` en `MarketjoyaBackend.sln`.
- [ ] Referencias según `references/dependency-rules.md`.
- [ ] `<Modulo>Module : IModule` en la raíz de Infrastructure (`Name` en minúsculas = schema; `Register` llama `AddCommonApplication`, `AddModuleDbContext`, repositorios y `AddReadDbConnection`).
- [ ] `<Modulo>DbContext : BaseDbContext` en `Infrastructure/Persistence` con `Schema` propio.
- [ ] `I<Modulo>ReadDbConnection` en `Application/Abstractions/` e implementación sobre `ReadDbConnection` en Infrastructure.
- [ ] Módulo y ensamblado Presentation registrados en `Marketjoya.Api/Hosting/ModuleCatalog.cs`.
- [ ] Primera migración generada con dotnet-ef en el schema del módulo.
- [ ] Proyectos `tests/Marketjoya.Modules.<Modulo>.UnitTests` e `IntegrationTests` en la solución.
- [ ] `Marketjoya.ArchitectureTests` pasa (descubre el módulo solo; no se editan los tests).
- [ ] Primer caso de uso sigue `checklists/new-use-case.md` y `templates/`.
- [ ] Sin referencias a otros módulos; contratos públicos van a `BuildingBlocks.Contracts` si hacen falta.
