# Plantilla: puertos y caso de uso

Rutas relativas a `MarketjoyaFront/`. Errores: `GoResult<T, AppError>` ([errors-and-results.md](../references/errors-and-results.md)).

## Puerto de entrada

Ruta: `src/core/<feature>/port/in/<operation>.port.ts`

```ts
import type { AppError } from '@base/app-error.type';
import type { GoResult } from '@base/go-result.type';
import type { ICreate<Feature>Command } from '@core/<feature>/domain/type/create-<feature>-command.type';
import type { I<Feature> } from '@core/<feature>/domain/type/<feature>.type';

export interface Create<Feature>Port {
  execute(command: ICreate<Feature>Command): Promise<GoResult<I<Feature>, AppError>>;
}
```

## Puerto de salida específico

Ruta: `src/core/<feature>/port/out/<capability>.port.ts`

Preferir capacidad concreta (`<Feature>Writer`, `<Feature>ByIdReader`) frente a un único `*Repository` CRUD.

```ts
export interface <Feature>WriterPort {
  create(command: ICreate<Feature>Command): Promise<GoResult<I<Feature>, AppError>>;
}
```

## Caso de uso

Ruta: `src/core/<feature>/application/use-case/create-<feature>.use-case.ts` (+ `.spec.ts` al lado)

```ts
export class Create<Feature>UseCase implements Create<Feature>Port {
  constructor(private readonly writer: <Feature>WriterPort) {}

  async execute(command: ICreate<Feature>Command): Promise<GoResult<I<Feature>, AppError>> {
    if (command.items.length === 0) {
      return goErr({
        type: 'VALIDATION',
        code: 'Create<Feature>.EmptyItems',
        description: 'La operación no tiene líneas.',
        fieldErrors: [],
      });
    }
    // El AppError del puerto se propaga intacto (type, code, fieldErrors, correlationId).
    return this.writer.create(command);
  }
}
```

- Ramifica por `error.type` y luego `error.code`; nunca por `description`.
- Un error propio del caso de uso usa un `code` estable `<Ámbito>.<Motivo>`.
- Sin Angular ni globals del navegador (lo rechaza el lint). Autorización como puerto o política, no como servicio de pantalla. Referencia real: `src/core/auth/`.
