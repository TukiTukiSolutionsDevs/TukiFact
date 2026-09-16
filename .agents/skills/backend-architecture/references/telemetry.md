# Telemetría y trazabilidad

La telemetría no se deja para después: es requisito del contrato. Stack: OpenTelemetry (OTLP) → Prometheus, Tempo, Loki, Grafana.

Checklist operativo: `checklists/telemetry.md`.

## Qué viene de Common (no reimplementar)

`AddCommonTelemetry()` (`Common.Infrastructure/Telemetry/`):

- Recurso identificado por `OTEL_SERVICE_NAME` (obligatorio). Exportador OTLP solo si `OTEL_EXPORTER_OTLP_ENDPOINT` está definido.
- Trazas: fuentes `Marketjoya.*`, `Marketjoya.Common.Application`, `Wolverine`, driver de Mongo; ASP.NET Core (sin `/health`), HttpClient y Npgsql (los spans de EF Core salen de Npgsql).
- Métricas: meters `Marketjoya.*` y `Wolverine:marketjoya`; ASP.NET Core, HttpClient, runtime y Npgsql.
- Logs por OpenTelemetry con scopes.

Behaviors del mediador (`Common.Application/Behaviors/`):

- `TelemetryBehavior`: `Activity` `marketjoya.usecase.<Modulo>.<CasoDeUso>` (módulo = segmento tras `Modules` en el namespace; caso de uso = nombre del request sin `Command`/`Query`). Tags: `marketjoya.module`, `marketjoya.usecase`, `marketjoya.user.id`, `marketjoya.company.ids`, `marketjoya.result` (`success`/`failure`/`exception`), `marketjoya.error.code`, `marketjoya.error.type`. Excepción: registrada y status `Error`.
- `LoggingBehavior`: scope `CorrelationId` + `UseCase`; registra inicio, éxito o fallo (código, tipo, duración). No registra el payload del request. Passwords, tokens y datos de tarjeta nunca se loguean.

`CorrelationIdMiddleware` (`Common.Presentation`): acepta `X-Correlation-Id` de 1–128 caracteres `[A-Za-z0-9._:-]`; si no, usa el trace id. Lo devuelve en el header, lo pone en el scope de logs y en el tag `marketjoya.correlation_id`.

Nunca crear spans/logs a mano para lo que los behaviors ya cubren.

## Regla única de nombres

Todo nombre propio lleva el prefijo `marketjoya`.

| Elemento | Regla | Ejemplo |
|---|---|---|
| `ActivitySource` / `Meter` propio | `Marketjoya.<Modulo>` (el host solo escucha `Marketjoya.*`) | `Marketjoya.Common.Application`, `Marketjoya.Billing` |
| `Activity` | `marketjoya.<ámbito>.<operación>` | `marketjoya.usecase.Billing.SubmitInvoice`, `marketjoya.sunat.submit` |
| Tag | `marketjoya.<atributo>` en minúsculas | `marketjoya.correlation_id`, `marketjoya.sunat.document_type` |
| Métrica | `marketjoya_<modulo>_<que>_<unidad>` en snake_case; labels en minúscula | `marketjoya_sales_completed_total` |

## Obligaciones de cada caso de uso

1. Traza base: el behavior la cubre.
2. Operación lenta o externa (SUNAT, OCR, GPS): `Activity` hijo con el `ActivitySource` del módulo.

```csharp
using var activity = BillingDiagnostics.Source.StartActivity("marketjoya.sunat.submit");
activity?.SetTag("marketjoya.sunat.document_type", document.Type);
```

3. Evento de negocio medible: emitir la métrica (siguiente sección).
4. Eventos de integración salen con `CorrelationId`: `IIntegrationEventPublisher` lo completa desde `ICorrelationIdAccessor`.

## Métricas de negocio

Viven en el `Meter` del módulo (`<Modulo>Diagnostics.Meter`, nombre `Marketjoya.<Modulo>`):

```csharp
public static class SalesDiagnostics
{
    public static readonly Meter Meter = new("Marketjoya.Sales");
    public static readonly Counter<long> SalesCompleted =
        Meter.CreateCounter<long>("marketjoya_sales_completed_total");
}

// En el handler del evento de dominio SaleCompleted (no en el command handler):
SalesCompleted.Add(1, new TagList { { "company", companyId }, { "register", registerId } });
```

Reglas:

- Cardinalidad baja: company, register, warehouse, medio_pago. Nunca ids de entidad ni timestamps como label.
- Se emiten desde handlers de eventos de dominio (el hecho ya ocurrió), no desde el command.
- Pendiente de decisión: ubicación de `<Modulo>Diagnostics`. Los handlers de dominio están en Application, que no puede referenciar Infrastructure; `System.Diagnostics` es BCL.

Métricas previstas (no implementadas en código): `marketjoya_outbox_pending`, `marketjoya_sync_lag_seconds`, `marketjoya_sync_conflicts_total`, `marketjoya_sunat_submission_duration_seconds`, `marketjoya_sunat_rejected_total`, `marketjoya_ai_quota_remaining_percent`, `marketjoya_saga_cycle_duration_minutes`. Revisar si la nueva ya está prevista.

## Cadena extremo a extremo

```text
HTTP request (X-Correlation-Id o trace id)
  → ICorrelationIdAccessor + scope de logs + tag marketjoya.correlation_id
  → Activity del caso de uso
  → evento de dominio → evento de integración en outbox (CorrelationId del evento y del envelope)
  → consumer Wolverine (correlation id restaurado en ICorrelationIdAccessor)
  → dead letter en Mongo con correlation_id si se agotan los reintentos
```

Con el correlation id se debe poder seguir una venta desde el escáner hasta SUNAT. Si un flujo lo pierde, es un bug.

## Auditoría de negocio

- Automático: `AuditableInterceptor` llena `created_by/at`, `updated_by/at` desde `IClock` e `ICurrentUser` en toda tabla raíz.
- Eventos auditables (ajuste de inventario, descuento autorizado, cierre de caja, decisión del motor multiempresa) archivados en Mongo `audit_events`: la colección está declarada (`AuditCollections.AuditEvents`) pero no existe `IAuditableDomainEvent` ni escritor. Pendiente de decisión: contrato y mecanismo de archivo.
