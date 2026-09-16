# Plantilla: tokens, módulo y provider

Rutas relativas a `MarketjoyaFront/`. Aún no hay ninguna feature cableada; esta forma sigue `src/data/README.md`.

## Token de entrada

Ruta: `src/data/<feature>/token/in/create-<feature>.token.ts` (solo importa `core`/`base`)

```ts
export const CREATE_<FEATURE>_TOKEN = new InjectionToken<Create<Feature>Port>(
  'CREATE_<FEATURE>_TOKEN',
);
```

## Token de salida

Ruta: `src/data/<feature>/token/out/<feature>-writer.token.ts` (UI nunca lo importa)

```ts
export const <FEATURE>_WRITER_TOKEN = new InjectionToken<<Feature>WriterPort>(
  '<FEATURE>_WRITER_TOKEN',
);
```

## Módulo data

Ruta: `src/data/<feature>/<feature>.module.ts`

```ts
@NgModule({
  providers: [
    ...<Feature>Provider,
    {
      provide: CREATE_<FEATURE>_TOKEN,
      useFactory: (writer: <Feature>WriterPort) => new Create<Feature>UseCase(writer),
      deps: [<FEATURE>_WRITER_TOKEN],
    },
  ],
})
export class <Feature>Module {}
```

## Provider infrastructure

Ruta: `src/infrastructure/<feature>/<feature>.provider.ts`

```ts
export const <Feature>Provider: Provider[] = [
  { provide: <FEATURE>_WRITER_TOKEN, useClass: <Feature>WriterAdapter },
];
```

El provider no registra casos de uso. Cuando exista la primera feature cableada, copia su forma y actualiza esta plantilla.
