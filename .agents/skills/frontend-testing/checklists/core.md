# Checklist: core

- [ ] Spec junto al use case, sin TestBed.
- [ ] Fake de puerto `out`, no del adapter HTTP.
- [ ] Éxito + al menos un `AppError` con `type` + `code` asertados.
- [ ] `AppError` del puerto propagado intacto (`toEqual`), incluido `correlationId`.
- [ ] Ninguna aserción ni rama depende de `description`.
- [ ] Permiso/invariante fuera del formulario.
- [ ] Nombre `execute_escenario_resultado`.
