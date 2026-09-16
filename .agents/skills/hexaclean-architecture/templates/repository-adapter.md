# Plantilla: adapter de puerto de salida

Ruta: `MarketjoyaFront/src/infrastructure/<feature>/adapter/out/<capability>.adapter.ts` (+ `.spec.ts` al lado)

```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { AppError } from '@base/app-error.type';
import { goErr, goOk } from '@base/go-result';
import type { GoResult } from '@base/go-result.type';
import { toAppError } from '@infrastructure/http/to-app-error.mapper';
import { firstValueFrom } from 'rxjs';

@Injectable()
export class <Feature>WriterAdapter implements <Feature>WriterPort {
  private readonly http = inject(HttpClient);

  // operationId: <Module>_<UseCase>
  async create(command: ICreate<Feature>Command): Promise<GoResult<I<Feature>, AppError>> {
    try {
      const dto = await firstValueFrom(
        this.http.post<<Feature>Dto>('/api/v1/<resource>', toRequestDto(command)),
      );
      return goOk(toDomain(dto));
    } catch (error: unknown) {
      return goErr(toAppError(error));
    }
  }
}
```

## Mapper compartido (`src/infrastructure/http/to-app-error.mapper.ts`)

Único punto que traduce ProblemDetails → `AppError`. Manda el status: `problem.type` (URI del RFC) nunca se lee como categoría. Forma mínima:

```ts
const RESERVED: ReadonlyMap<number, readonly [ErrorType, string]> = new Map([
  [HttpStatusCode.BadRequest, ['VALIDATION', 'Http.BadRequest']],
  [HttpStatusCode.Unauthorized, ['UNAUTHORIZED', 'Auth.Unauthorized']],
  [HttpStatusCode.Forbidden, ['FORBIDDEN', 'Auth.Forbidden']],
  [HttpStatusCode.NotFound, ['NOT_FOUND', 'Http.NotFound']],
  [HttpStatusCode.Conflict, ['CONFLICT', 'Http.Conflict']],
  [HttpStatusCode.RequestTimeout, ['NETWORK', 'Network.Timeout']],
  [HttpStatusCode.TooManyRequests, ['NETWORK', 'Network.RateLimited']], // respetar Retry-After al reintentar
]);

// sentCorrelationId: X-Correlation-Id generado por el interceptor para este request.
export function toAppError(error: unknown, sentCorrelationId?: string): AppError {
  if (error instanceof TimeoutError) return networkError('Network.Timeout');
  if (!(error instanceof HttpErrorResponse)) return unknownError();
  if (error.status === 0) return networkError('Network.Unavailable');

  const problem = readProblemDetails(error.error); // null si el cuerpo falta o no es un objeto JSON
  const reserved = error.status >= 500 ? (['FAILURE', 'Server.Failure'] as const) : RESERVED.get(error.status);
  if (reserved === undefined) return unknownError(error, problem); // status fuera de §2 (3xx, 418, 422): problem?.code ?? 'Unknown.Unexpected'

  const [type, fallbackCode] = reserved;
  return {
    type,
    code: problem?.code ?? fallbackCode,
    description: problem?.detail ?? problem?.title ?? '',
    fieldErrors: error.status === HttpStatusCode.BadRequest ? (problem?.errors ?? []) : [],
    status: error.status,
    traceId: problem?.traceId,
    correlationId: problem?.correlationId ?? error.headers.get('X-Correlation-Id') ?? sentCorrelationId,
    retryAfterSeconds: readRetryAfterSeconds(error), // solo 429/503; segundos o HTTP-date → segundos
  };
}
```

Tabla completa y reglas: `../references/errors-and-results.md`.

## Reglas

- Path, verbo y DTO del OpenAPI (`../references/api-contract.md`). Enums llegan como números: el mapper los traduce.
- HTTP, DTO y mapper solo en infrastructure. No serialices DTOs como dominio.
- El adapter no parsea ProblemDetails por su cuenta: siempre `toAppError`. `field` es la ruta del body del request; si el formulario usa otra, tradúcela aquí.
- Sin router, toast ni componentes. Un adapter = un puerto de capacidad.
- Si el mapper cambia, cambia solo `to-app-error.mapper.ts`, no cada adapter.
