# Mega-Sprint 1: Completar el Motor

> **Tema**: NATS Consumers + Email Auto + Notificaciones + Rate Limiting
> **Estimacion**: ~2-3 dias
> **Prioridad**: PRIMERA — sin esto los webhooks y emails nunca se disparan automaticamente

---

## Contexto

Ya existe:
- `NatsEventPublisher.cs` — publica eventos a NATS
- `WebhookDeliveryService.cs` — dispara webhooks con retries
- `EmailService.cs` — envia emails
- `InMemoryRateLimiter.cs` — rate limiter basico
- `WebhooksController.cs` — CRUD de webhook configs

Lo que FALTA es el **consumer side** — nadie escucha los eventos publicados.
Es como tener un altavoz encendido pero nadie con auriculares.

---

## Tareas

### M1.1 — NATS JetStream Consumers (ALTO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Infrastructure/Services/NatsConsumerHostedService.cs` — BackgroundService que escucha NATS
- `src/TukiFact.Infrastructure/Services/EventHandlers/DocumentCreatedHandler.cs`
- `src/TukiFact.Infrastructure/Services/EventHandlers/DocumentSentHandler.cs`
- `src/TukiFact.Infrastructure/Services/EventHandlers/DocumentFailedHandler.cs`
- `src/TukiFact.Api/Program.cs` — registrar hosted service

**Que hace**:
```
NATS JetStream Stream: "tukifact-events"
Subjects:
  - document.created   → log + notificacion
  - document.sent      → webhook + email PDF + notificacion
  - document.failed    → webhook + notificacion + retry logic
  - document.voided    → webhook + notificacion
  - quotation.created  → notificacion
  - quotation.converted → notificacion
  - retention.created  → webhook + notificacion
  - perception.created → webhook + notificacion
  - despatch.emitted   → webhook + notificacion
```

**Patron**:
```csharp
// Cada handler implementa:
public interface IEventHandler
{
    string Subject { get; }
    Task HandleAsync(NatsMsg<byte[]> msg, CancellationToken ct);
}

// El HostedService:
// 1. Crea JetStream stream si no existe
// 2. Crea consumer durable por subject
// 3. Loop infinito consumiendo mensajes
// 4. Despacha al handler correcto
// 5. Ack manual tras procesamiento exitoso
```

### M1.2 — Email Trigger Automatico (MEDIO)
**Dependencias**: M1.1
**Archivos a crear/modificar**:
- `src/TukiFact.Infrastructure/Services/EventHandlers/DocumentSentHandler.cs` — agregar logica email
- `src/TukiFact.Infrastructure/Services/EmailService.cs` — verificar template PDF attachment

**Flujo**:
```
document.sent event
  → DocumentSentHandler
    → Buscar document por ID
    → Buscar customer email
    → Si customer tiene email:
      → Generar PDF (PdfGenerator)
      → Enviar email con PDF adjunto (EmailService)
      → Crear EmailLog
    → Si no tiene email: skip + log
```

**Configuracion por tenant**:
- Setting: `auto_send_email` (true/false) en TenantServiceConfig
- Template email: HTML basico con logo empresa + link descarga PDF

### M1.3 — Notificaciones In-App SSE (ALTO)
**Dependencias**: M1.1
**Archivos a crear/modificar**:
- `src/TukiFact.Domain/Entities/Notification.cs` — nueva entidad
- `src/TukiFact.Domain/Interfaces/INotificationRepository.cs`
- `src/TukiFact.Infrastructure/Persistence/Repositories/NotificationRepository.cs`
- `src/TukiFact.Infrastructure/Services/NotificationService.cs`
- `src/TukiFact.Api/Controllers/NotificationsController.cs`
- Migration nueva para tabla `notifications`

**Endpoints**:
```
GET  /v1/notifications          — lista paginada (ultimas 50)
GET  /v1/notifications/stream   — SSE endpoint (Server-Sent Events)
PUT  /v1/notifications/:id/read — marcar como leida
PUT  /v1/notifications/read-all — marcar todas como leidas
GET  /v1/notifications/unread-count — contador badge
```

**SSE Flow**:
```
Cliente abre conexion SSE → /v1/notifications/stream
  → Servidor mantiene conexion abierta
  → Cuando NATS consumer recibe evento del tenant:
    → Crea Notification en DB
    → Publica via SSE al cliente conectado
  → Cliente recibe: { type: "document.sent", title: "Factura F001-123 enviada", ... }
```

**Notification Entity**:
```csharp
public class Notification
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }  // null = para todos del tenant
    public string Type { get; set; }    // document.sent, document.failed, etc.
    public string Title { get; set; }
    public string? Body { get; set; }
    public string? EntityType { get; set; }  // Document, Quotation, etc.
    public Guid? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

### M1.4 — Webhook Delivery Dispatcher (MEDIO)
**Dependencias**: M1.1
**Archivos a crear/modificar**:
- `src/TukiFact.Infrastructure/Services/EventHandlers/WebhookDispatchHandler.cs`
- `src/TukiFact.Infrastructure/Services/WebhookDeliveryService.cs` — verificar retry logic

**Flujo**:
```
Cualquier evento NATS
  → WebhookDispatchHandler
    → Buscar WebhookConfigs del tenant que tienen ese event type
    → Para cada config activa:
      → WebhookDeliveryService.DeliverAsync(config, eventPayload)
        → POST a config.Url con HMAC signature
        → Si falla: schedule retry (1min, 5min, 30min, 2h, 24h)
        → Crear WebhookDelivery log
```

**Payload webhook**:
```json
{
  "event": "document.sent",
  "timestamp": "2026-04-14T...",
  "data": {
    "document_id": "...",
    "type": "01",
    "serie": "F001",
    "correlativo": 123,
    "total": 1180.00,
    "status": "sent",
    "sunat_response_code": "0"
  }
}
```

**Headers**:
```
X-TukiFact-Event: document.sent
X-TukiFact-Signature: sha256=...
X-TukiFact-Delivery: {delivery_id}
X-TukiFact-Timestamp: {unix_timestamp}
```

### M1.5 — Rate Limiting Middleware (BAJO)
**Dependencias**: ninguna (paralelo)
**Archivos a crear/modificar**:
- `src/TukiFact.Api/Middleware/RateLimitingMiddleware.cs`
- `src/TukiFact.Api/Program.cs` — registrar middleware
- `src/TukiFact.Infrastructure/Services/InMemoryRateLimiter.cs` — agregar limites por plan

**Limites por plan**:
```
Free:          100 requests/hora
Emprendedor:   500 requests/hora
Negocio:     2,000 requests/hora
Developer:   2,000 requests/hora
Profesional: 5,000 requests/hora
Empresa:    20,000 requests/hora
```

**Headers de respuesta**:
```
X-RateLimit-Limit: 500
X-RateLimit-Remaining: 487
X-RateLimit-Reset: 1713100800
```

**429 Too Many Requests** cuando se excede.

### M1.6 — Frontend Notificaciones (MEDIO)
**Dependencias**: M1.3
**Archivos a crear/modificar**:
- `src/tukifact-web/src/components/notifications/NotificationBell.tsx`
- `src/tukifact-web/src/components/notifications/NotificationDropdown.tsx`
- `src/tukifact-web/src/lib/useNotifications.ts` — hook SSE
- `src/tukifact-web/src/app/(authenticated)/layout.tsx` — agregar bell al header

**Comportamiento**:
- Bell icon en el header con badge de unread count
- Click → dropdown con ultimas 10 notificaciones
- Click en notificacion → navega a la entidad (ej: /documents/{id})
- "Marcar todas como leidas"
- SSE mantiene actualizado en tiempo real
- Sound effect opcional en nueva notificacion

---

## Criterios de Completado

- [ ] NATS JetStream stream creado automaticamente al iniciar
- [ ] Consumer durable escuchando 9+ subjects
- [ ] Webhook se dispara automaticamente cuando se emite un documento
- [ ] Email con PDF se envia automaticamente al emitir (si tenant lo tiene activo)
- [ ] Notificaciones aparecen en tiempo real en el frontend
- [ ] Rate limiting activo por tenant segun plan
- [ ] 0 errores lint, 0 warnings
- [ ] Build limpio frontend + backend

---

## Dependencia con otros Mega-Sprints

```
M1.1 (NATS consumers) ──→ M1.2 (email auto)
                       ──→ M1.3 (notificaciones)
                       ──→ M1.4 (webhook dispatch)
M1.5 (rate limit) ──→ M4.4 (rate limit por plan) [Mega-Sprint 4]
M1.3 (notificaciones) ──→ M1.6 (frontend bell)
```
