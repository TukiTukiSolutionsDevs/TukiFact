# Checklist: architecture tests

- [ ] `Marketjoya.ArchitectureTests` pasa.
- [ ] Módulo nuevo: proyectos con el esquema `Marketjoya.Modules.<M>.<Capa>` y en la solución (`SolutionDiscoveryTests`); no se editan listas de ensamblados.
- [ ] Handlers `internal sealed` junto a su request; validators `internal sealed <Mensaje>Validator`.
- [ ] Domain sin paquetes; handlers sin `DbContext`; query handlers sin repositorios ni `ITransactionManager`.
- [ ] Repositorios ≤ 5 métodos sin `IQueryable`; sin `IUnitOfWork`; Presentation sin acceso a datos.
- [ ] Regla nueva: implementación en `Rules/`, test en `Dependencies/` o `Conventions/`, violación en `Fixtures/Modules` y test en `SelfCheck/`.
- [ ] No se apagó una regla para hacer pasar un merge.
