# Checklist: migración de adapter

- [ ] Puerto de salida permanece estable.
- [ ] Nuevo adapter solo depende de core/base/environment/tokens de data (lint).
- [ ] HTTP/SDK/storage encapsulado en infrastructure.
- [ ] DTOs separados de modelos de dominio y alineados al OpenAPI.
- [ ] Mapper cubre respuesta, fechas, enums numéricos y campos opcionales.
- [ ] Errores externos convertidos con `toAppError`; `type`, `code`, `fieldErrors` y `correlationId` iguales a los del adapter anterior.
- [ ] Provider cambia binding sin tocar UI.
- [ ] Interceptores registrados una sola vez en `src/app.config.ts`.
- [ ] No se filtran secretos ni tokens al log.
- [ ] Spec del adapter con `HttpTestingController`.
- [ ] Fallback/migración de datos persistidos considerado.
