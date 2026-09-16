# Checklist: Domain

- [ ] El test está en `UnitTests/<Submodulo>/` y espeja el tipo de Domain.
- [ ] No hay IO: ni DbContext, ni HTTP, ni Testcontainers.
- [ ] Builder + Bogus; no hay objetos armados campo a campo.
- [ ] Nombre `Metodo_Escenario_ResultadoEsperado` y AAA.
- [ ] Cubiertos: happy path, invariantes rotas, evento de dominio solo en éxito.
- [ ] `Result` o `BusinessRuleValidationException` según el contrato de Domain; fallos asertados por `Error.Code` y `Error.Type`, no por `Description`.
- [ ] `[Theory]` solo para variaciones del mismo comportamiento.
- [ ] Si es StockItem/Sale/CreditLine/CashClosing/granel/multiempresa: escenarios de riesgo completos.
