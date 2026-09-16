# Checklist: componente nuevo

- [ ] ¿Es primitivo (`design-system/components/`) o de negocio (`design-system/business-components/`)?
- [ ] JSON válido según `design-system/schemas/component.schema.json`.
- [ ] Variantes y estados listados; tamaños con `{token}` no literales sueltos salvo altura de plataforma.
- [ ] Nombre `Mr*`.
- [ ] Renderer Angular y/o Compose; no solo uso ad-hoc en una Screen.
- [ ] No duplica un `Mr*` existente.
