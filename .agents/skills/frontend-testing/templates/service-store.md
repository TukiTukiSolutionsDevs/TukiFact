# Plantilla: service / store

```ts
it('execute_success_replacesStore', async () => {
  const facade = { execute: async () => goOk({ id: '1' }) };
  const store = new <Feature>Store();
  const service = new <Feature>Service(facade as <Feature>Facade, store, feedback);

  await service.execute({ name: 'x' });

  expect(store.entity()).toEqual({ id: '1' });
  expect(store.operation()).toEqual({ status: 'success' });
});

it('execute_failure_keepsTypeCodeAndShowsCorrelationId', async () => {
  const failure: AppError = { type: 'FAILURE', code: 'Server.Failure', description: '', fieldErrors: [], correlationId: 'web-1' };
  const facade = { execute: async () => goErr(failure) };
  const store = new <Feature>Store();
  const service = new <Feature>Service(facade as <Feature>Facade, store, feedback);

  await service.execute({ name: 'x' });

  expect(store.operation()).toEqual({ status: 'error', error: failure });
  expect(store.entity()).toBeNull();
  expect(feedback.lastMessage()?.supportReference).toBe('web-1');
});

it('execute_validation_setsFieldErrors', async () => {
  const fieldErrors = [{ field: 'name', code: 'Name.Required', description: 'Requerido.' }];
  const facade = { execute: async () => goErr({ type: 'VALIDATION', code: 'Create<Feature>.Validation', description: '', fieldErrors }) };
  const store = new <Feature>Store();
  const service = new <Feature>Service(facade as <Feature>Facade, store, feedback);

  await service.execute({ name: '' });

  expect(store.fieldErrors()).toEqual(fieldErrors);
});
```

- El facade es fake. No instancies el adapter.
- `feedback` es un fake in-memory que registra el último mensaje.
- Mismo `type` + `code` con distinta `description` → mismo mensaje local: prueba que no se ramifica por `description`.
