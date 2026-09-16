# Checklist: Application

- [ ] Test en `Marketjoya.Modules.<M>.IntegrationTests/<Submodulo>/<CasoDeUso>/`, carpeta espejo.
- [ ] Fixture de colección derivado de `ModuleIntegrationFixture`; se envía con `fixture.SendAsync`; el handler no se instancia con repositorios mockeados.
- [ ] Postgres de la colección (un contenedor, migrado una vez). Sin limpieza/create por `[Fact]`.
- [ ] Datos del test con identidad única; asserts sobre esa fila, no sobre conteos de tabla.
- [ ] Dobles solo en `ConfigureTestServices`; NSubstitute solo para SUNAT, OCR o GPS.
- [ ] Éxito: `Result.Success` y efecto persistido.
- [ ] Fallo de negocio: `Result.Failure` con `Error.Code` y `Error.Type` del estático, sin mutación; nunca se aserta `Description`.
- [ ] Command inválido (si hay validator): `Error.Code` `<CasoDeUso>.Validation`, `Type` `Validation` y `FieldErrors` con `Field` y `Code`.
- [ ] Query: schema propio, not found cubierto; sin EF ni agregados.
- [ ] Si aplica sync: `client_mutation_id` duplicado = una sola mutación.
- [ ] Si aplica integración: outbox verificado (o test hermano en Infrastructure).
- [ ] El test no llama `SaveChanges` ni envuelve el handler en una transacción extra.
