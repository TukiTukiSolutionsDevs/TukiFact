# Plantilla: test de use case (core)

`MarketjoyaFront/src/core/<feature>/application/use-case/create-<feature>.use-case.spec.ts`

```ts
import type { AppError } from '@base/app-error.type';
import { goErr, goOk } from '@base/go-result';
import type { GoResult } from '@base/go-result.type';
import { Create<Feature>UseCase } from './create-<feature>.use-case';

const appError = (type: AppError['type'], code: string): AppError => ({
  type,
  code,
  description: 'texto irrelevante para la lógica',
  fieldErrors: [],
  correlationId: 'web-1',
});

describe('Create<Feature>UseCase', () => {
  it('execute_validCommand_returnsEntity', async () => {
    // Arrange
    const writer: <Feature>WriterPort = {
      create: async (cmd) => goOk({ id: crypto.randomUUID(), ...cmd }),
    };
    const useCase = new Create<Feature>UseCase(writer);

    // Act
    const [data, error] = await useCase.execute({ name: 'x' });

    // Assert
    expect(error).toBeNull();
    expect(data?.name).toBe('x');
  });

  it.each([
    appError('UNAUTHORIZED', 'Auth.Unauthorized'),
    appError('FORBIDDEN', 'Auth.Forbidden'),
    appError('CONFLICT', 'Concurrency.Conflict'),
    appError('NETWORK', 'Network.Unavailable'),
  ])('execute_writerFails$code_propagatesSameError', async (failure) => {
    // Arrange
    const writer: <Feature>WriterPort = {
      create: async (): Promise<GoResult<I<Feature>, AppError>> => goErr(failure),
    };

    // Act
    const [, error] = await new Create<Feature>UseCase(writer).execute({ name: 'x' });

    // Assert
    expect(error).toEqual(failure); // type, code, fieldErrors y correlationId intactos
  });
});
```

- Sin `TestBed`. `describe`/`it`/`expect` son globals. El fake implementa el puerto, no el adapter.
- Error propio del caso de uso: asserta `type` y `code`, nunca `description`.
