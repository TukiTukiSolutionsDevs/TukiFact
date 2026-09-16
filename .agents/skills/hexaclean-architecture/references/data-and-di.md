# Data y DI

```ts
{
  provide: CREATE_<FEATURE>_TOKEN,
  useFactory: (writer: <Feature>WriterPort) =>
    new Create<Feature>UseCase(writer),
  deps: [<FEATURE>_WRITER_TOKEN],
}
```

El módulo `data` registra los providers de infrastructure y los casos de uso. El provider registra adapters, nunca casos de uso. Forma completa: [templates/data-and-provider.md](../templates/data-and-provider.md).

## Estructura (`src/data/<feature>/`)

- `token/in/<operation>.token.ts`: lo único de `data` que UI puede importar.
- `token/out/<capability>.token.ts`: solo `data` e `infrastructure`.
- `<feature>.module.ts`: composición. Los tokens `in`/`out` solo importan `core` y `base` (lint).

## Composición raíz

`src/app.config.ts` registra una sola vez `provideHttpClient(withFetch(), withInterceptors([correlationIdInterceptor]))` y el router. Las features no vuelven a llamar `provideHttpClient`. `correlationIdInterceptor` (`src/infrastructure/http/correlation-id.interceptor.ts`, junto a `CORRELATION_ID_HEADER` y `createCorrelationId`) genera y envía `X-Correlation-Id` en todo request ([api-contract.md](api-contract.md)).

## Scope

Decide scope al crear la feature (layout/ruta de feature vs raíz). Evita providers globales sin necesidad.

## Checklist DI

- Token tipado con puerto.
- Factory con `deps` explícitas.
- Provider separado del módulo data.
- Adapter no se instancia desde UI.
- Scope = lifecycle del store.
- Cliente HTTP registrado solo en `src/app.config.ts`.
- Puertos de salida por capacidad, no un token CRUD genérico.
