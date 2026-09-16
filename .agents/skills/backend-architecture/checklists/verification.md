# Checklist: verificación

Desde `MarketjoyaBackend/` (mismos pasos que `.github/workflows/backend-ci.yml`):

```bash
dotnet format --verify-no-changes
dotnet build -c Release                         # regenera openapi/marketjoya-api-v1.json
git diff --exit-code -- openapi/                # contrato commiteado
dotnet test                                     # arquitectura, unit, integración, E2E (requiere Docker)
dotnet list package --vulnerable --include-transitive
```

- [ ] `Marketjoya.ArchitectureTests` en verde (dependencias, handlers, repositorios, carpetas, consumers, endpoints).
- [ ] Migración EF del módulo si cambió el modelo de escritura (`references/common-shared-kernel.md`, Migraciones); nunca `Database.Migrate()` al arrancar.
- [ ] Query SQL revisada: schema propio, sin joins a schemas ajenos.
- [ ] Endpoint: `WithName`, `WithSummary`, `WithStandardProblems`, autorización, sin `WithTags`, `ToHttpResult()` sin `ProblemDetails` a mano.
- [ ] ProblemDetails según `references/api-error-contract.md` §4: `code`, `traceId`, `correlationId`; 400 con `errors`.
- [ ] `code` estable: ningún código existente renombrado (es API pública para frontend y mobile).
- [ ] `openapi/marketjoya-api-v1.json` commiteado con el endpoint.
- [ ] Comando: transacción a cargo de `TransactionBehavior`; eventos de dominio despachados en `SaveChangesAsync`.
- [ ] Evento de integración publicado con `IIntegrationEventPublisher` desde un handler de dominio.
- [ ] Logs sin passwords, tokens ni datos de tarjeta.
- [ ] Tests de la rebanada según `../backend-testing/checklists/verification.md`.
- [ ] Informar cualquier comando no ejecutado y su motivo.
