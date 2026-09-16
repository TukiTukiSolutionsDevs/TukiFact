# Checklist: Infrastructure

- [ ] Roundtrip del agregado cubierto (aquí o por el command de Application).
- [ ] Misma colección/DB que Application; sin create/limpieza por test.
- [ ] Schema propio; no se lee otro módulo.
- [ ] Si hay evento de integración: enviado tras el commit y descartado en rollback.
- [ ] Si hay consumer: inbox idempotente (mismo mensaje ×2).
- [ ] Retry o DLQ (Postgres + Mongo `dead_letters`) cubierto cuando el contrato del consumer lo define.
- [ ] Espera con polling + timeout (`Eventually.SatisfiesAsync`); no hay `Thread.Sleep`.
- [ ] Cliente HTTP externo no pega al proveedor real (NSubstitute).
- [ ] No se re-prueba lógica de dominio dentro del consumer.
