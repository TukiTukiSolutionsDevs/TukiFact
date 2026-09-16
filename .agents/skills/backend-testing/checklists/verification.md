# Checklist: verificación de tests

- [ ] Proyecto estrecho de la capa (UnitTests o IntegrationTests del módulo).
- [ ] Architecture tests si cambió una frontera o hay un módulo nuevo.
- [ ] E2E solo si el flujo es crítico o el endpoint es nuevo en un flujo ya E2E.
- [ ] Comando ejecutado anotado; si se saltó integración/E2E, el motivo (sin Docker, sin runner).
- [ ] Ningún test nuevo usa `Thread.Sleep`, mocks de repositorio, carpetas horizontales, ni create/migración/limpieza de DB por `[Fact]`.
- [ ] Proyecto de test nuevo agregado a `MarketjoyaBackend.sln`.

Desde `MarketjoyaBackend/` (Microsoft.Testing.Platform):

```bash
dotnet test --project tests/Marketjoya.Modules.<M>.UnitTests
dotnet test --project tests/Marketjoya.Modules.<M>.IntegrationTests
dotnet test --project tests/Marketjoya.ArchitectureTests
dotnet test --project tests/Marketjoya.Api.E2ETests
dotnet test --coverlet --coverlet-output-format cobertura --results-directory TestResults   # todas, con cobertura
```
