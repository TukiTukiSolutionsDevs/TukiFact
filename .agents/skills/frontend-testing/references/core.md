# Capa: core

Sin Angular, sin `HttpClient`, sin `TestBed`. Instancias puras.

## Qué probar

| Pieza | Demostrar |
|---|---|
| Use case | Invariante, permiso de aplicación, coordinación de puertos |
| Executor / política | Reintentos, concurrencia; decide por `error.type` y deja pasar el `AppError` intacto |
| Puerto (contrato) | El fake del test implementa el puerto; el use case no ve el adapter |
| `GoResult` / error | Éxito y cada `type` + `code` que el caso de uso puede devolver; el `AppError` del puerto se propaga igual (`toEqual`) |

Asserta `type` y `code`; nunca `description`. Contrato: [api-error-contract.md](../../backend-architecture/references/api-error-contract.md).

## Qué no probar

Tipos vacíos, aliases, `goOk`/`goErr` (ya cubiertos en `src/base/go-result.spec.ts`).

## Cómo

```ts
const writer: <Feature>WriterPort = {
  create: async (cmd) => goOk({ id: 'sale-1', ...cmd }),
};
const useCase = new Create<Feature>UseCase(writer);
const [data, error] = await useCase.execute(command);
```

Varios escenarios → `it.each`. Concurrencia: fake con promesas diferidas y aserción sobre `calls`.

Referencias reales: `MarketjoyaFront/src/core/auth/application/use-case/refresh-session.use-case.spec.ts`, `MarketjoyaFront/src/core/auth/application/executor/refresh-use-case.executor.spec.ts`.

Checklist: `checklists/core.md`. Plantilla: `templates/use-case.md`.
