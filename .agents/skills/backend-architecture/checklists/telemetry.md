# Checklist: telemetría y trazabilidad

- [ ] No se reimplementa traza base: `TelemetryBehavior` y `LoggingBehavior` ya cubren el caso de uso.
- [ ] Operación externa/lenta (SUNAT, OCR, GPS) tiene `Activity` hijo del `ActivitySource` `Marketjoya.<Modulo>` con tags `marketjoya.*`.
- [ ] Hecho de negocio medible emite métrica desde el handler del evento de dominio, no desde el command.
- [ ] Métrica: `marketjoya_<modulo>_<que>_<unidad>`, cardinalidad baja, sin ids ni timestamps como label.
- [ ] Se revisó si la métrica ya está prevista antes de crear otra.
- [ ] Evento de integración publicado con `IIntegrationEventPublisher` (completa `CorrelationId` desde `ICorrelationIdAccessor`).
- [ ] Hecho auditable por gerencia: señalarlo; el mecanismo `audit_events` está pendiente de decisión.
- [ ] Logs sin datos sensibles.
