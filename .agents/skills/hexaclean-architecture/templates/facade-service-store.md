# Plantilla: facade, service y store

Ruta: `MarketjoyaFront/src/ui/<feature>/` (`<feature>.facade.ts`, `<feature>.service.ts`, `<feature>.store.ts`, specs al lado).

## Facade

```ts
@Injectable()
export class <Feature>Facade {
  private readonly operation = inject(<OPERATION>_TOKEN); // @data/<feature>/token/in/...

  execute(command: <Command>): Promise<GoResult<I<Feature>, AppError>> {
    return this.operation.execute(command);
  }
}
```

La facade no navega, no muestra toast, no actualiza el store ni interpreta el `AppError`.

## Service

```ts
async execute(command: <Command>): Promise<void> {
  this.store.setOperation({ status: 'loading' });
  const [data, error] = await this.facade.execute(command);
  if (error !== null) {
    this.store.setOperation({ status: 'error', error });
    if (error.type === 'VALIDATION') {
      this.store.setFieldErrors(error.fieldErrors); // se pintan en su campo
    }
    this.feedback.showError(toUserMessage(error, this.texts));
    return;
  }
  this.store.replace(data);
  this.store.setOperation({ status: 'success' });
}
```

`error !== null` estrecha `data`. `UNAUTHORIZED` lo resuelve el executor de refresh antes de llegar aquí ([refresh-session.md](../references/refresh-session.md)).

## Mensaje al usuario (helper de UI)

```ts
export function toUserMessage(error: AppError, texts: Readonly<Record<string, string>>): IUserMessage {
  const local = texts[error.code];
  switch (error.type) {
    case 'FAILURE':
    case 'NETWORK':
    case 'UNKNOWN':
      return { text: local ?? GENERIC_BY_TYPE[error.type], supportReference: error.correlationId };
    default:
      return { text: local ?? (error.description || GENERIC_BY_TYPE[error.type]) };
  }
}
```

- `GENERIC_BY_TYPE: Readonly<Record<ErrorType, string>>`: el compilador exige los 8 valores.
- Ramifica por `type` y `code`; nunca por `description`. `supportReference` visible en pantalla.
- Reglas completas: [errors-and-results.md](../references/errors-and-results.md).

## Store

`signal`/`computed`; expón solo lectura. Guarda el `AppError` completo en el estado `error`. Separa estados por operación si pueden concurrir. Scope del provider en ruta, layout o screen, de forma intencional.
