# Plantilla: adapter HTTP

`MarketjoyaFront/src/infrastructure/<feature>/adapter/out/<capability>.adapter.spec.ts`

```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { <Feature>WriterAdapter } from './<feature>-writer.adapter';

const PROBLEM_HEADERS = { 'Content-Type': 'application/problem+json' };

describe('<Feature>WriterAdapter', () => {
  let adapter: <Feature>WriterAdapter;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), <Feature>WriterAdapter],
    });
    adapter = TestBed.inject(<Feature>WriterAdapter);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('create_conflictWithCode_returnsTypeAndCode', async () => {
    // Act
    const pending = adapter.create({ name: 'x' });
    http.expectOne('/api/v1/<resource>').flush(
      {
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10', // URI del RFC: no es la categoría
        status: 409,
        detail: 'Versión desactualizada.',
        code: 'Concurrency.Conflict',
        traceId: 't-1',
        correlationId: 'web-1',
      },
      { status: 409, statusText: 'Conflict', headers: PROBLEM_HEADERS },
    );

    // Assert
    const [data, error] = await pending;
    expect(data).toBeNull();
    expect(error).toMatchObject({ type: 'CONFLICT', code: 'Concurrency.Conflict', status: 409, correlationId: 'web-1' });
  });

  it('create_validationWithErrors_returnsFieldErrors', async () => {
    // Act
    const pending = adapter.create({ name: '' });
    http.expectOne('/api/v1/<resource>').flush(
      { status: 400, code: 'Create<Feature>.Validation', errors: [{ field: 'name', code: 'Name.Required', description: 'Requerido.' }] },
      { status: 400, statusText: 'Bad Request', headers: PROBLEM_HEADERS },
    );

    // Assert
    const [, error] = await pending;
    expect(error?.type).toBe('VALIDATION');
    expect(error?.fieldErrors).toEqual([{ field: 'name', code: 'Name.Required', description: 'Requerido.' }]);
  });

  it.each([
    [400, 'VALIDATION', 'Http.BadRequest'],
    [401, 'UNAUTHORIZED', 'Auth.Unauthorized'],
    [403, 'FORBIDDEN', 'Auth.Forbidden'],
    [404, 'NOT_FOUND', 'Http.NotFound'],
    [409, 'CONFLICT', 'Http.Conflict'],
    [408, 'NETWORK', 'Network.Timeout'],
    [500, 'FAILURE', 'Server.Failure'],
  ] as const)('create_status%iWithoutCode_returns%s', async (status, type, code) => {
    // Act
    const pending = adapter.create({ name: 'x' });
    http.expectOne('/api/v1/<resource>').flush(null, { status, statusText: 'Error' });

    // Assert
    expect((await pending)[1]).toMatchObject({ type, code, status, fieldErrors: [] });
  });

  it('create_rateLimitedPlainText_returnsNetworkRateLimited', async () => {
    // Act
    const pending = adapter.create({ name: 'x' });
    http.expectOne('/api/v1/<resource>').flush('Too Many Requests', {
      status: 429,
      statusText: 'Too Many Requests',
      headers: { 'Retry-After': '30' },
    });

    // Assert
    expect((await pending)[1]).toMatchObject({
      type: 'NETWORK',
      code: 'Network.RateLimited',
      status: 429,
      retryAfterSeconds: 30,
    });
  });

  it('create_badGatewayHtmlBody_returnsFailure', async () => {
    // Act
    const pending = adapter.create({ name: 'x' });
    http.expectOne('/api/v1/<resource>').flush('<html>Bad Gateway</html>', { status: 502, statusText: 'Bad Gateway' });

    // Assert
    expect((await pending)[1]).toMatchObject({ type: 'FAILURE', code: 'Server.Failure', status: 502 });
  });

  it('create_statusOutsideContractWithCode_returnsUnknownKeepingCode', async () => {
    // Act
    const pending = adapter.create({ name: 'x' });
    http.expectOne('/api/v1/<resource>').flush(
      { status: 422, code: 'Sale.Unprocessable' },
      { status: 422, statusText: 'Unprocessable Content', headers: PROBLEM_HEADERS },
    );

    // Assert
    expect((await pending)[1]).toMatchObject({ type: 'UNKNOWN', code: 'Sale.Unprocessable', status: 422 });
  });

  it('create_statusOutsideContractNonJsonBody_returnsUnknown', async () => {
    // Act
    const pending = adapter.create({ name: 'x' });
    http.expectOne('/api/v1/<resource>').flush('<html>teapot</html>', { status: 418, statusText: "I'm a teapot" });

    // Assert
    expect((await pending)[1]).toMatchObject({ type: 'UNKNOWN', code: 'Unknown.Unexpected', status: 418 });
  });

  it('create_networkError_returnsNetwork', async () => {
    // Act
    const pending = adapter.create({ name: 'x' });
    http.expectOne('/api/v1/<resource>').error(new ProgressEvent('error'));

    // Assert
    expect((await pending)[1]).toMatchObject({ type: 'NETWORK', code: 'Network.Unavailable' });
  });
});
```

- Llamar → `expectOne` → `flush`/`error` → await. Path del OpenAPI (`/api/v1/...`).
- Tabla completa del contrato §7: en el spec del mapper compartido (`../references/infrastructure.md`). En un adapter concreto basta éxito + los errores que su operación produce.
- Manda el status: `AppError.type` sale del status, nunca del `type` URI del ProblemDetails. `UNKNOWN` solo con status fuera de §2 y cuerpo no interpretable.
- Asserta `type` y `code`; `description` nunca decide la clasificación.
