# TukiFact — Master Plan Pre-Produccion

> **Objetivo**: Completar TODO lo pendiente ANTES del deploy a VPS.
> Cuando este plan esté 100%, solo queda: PRD + Testing completo → Deploy VPS.
>
> _Creado: 2026-04-14_

---

## Estado Actual (78% completado)

```
FASE 1  ████████████████████████ 100%  Fundacion (Sprints 1-4)
FASE 2  █████████████████░░░░░░  75%   Produccion (Sprints 5-8)
FASE 3  ░░░░░░░░░░░░░░░░░░░░░░   0%   Crecimiento (Sprints 9-12)
EXTRA   ████████████████░░░░░░░  66%   Backoffice (B1+B2 done, B3 pending)
MEJORA  ████████████████████████ 100%  Batch A+B+C
```

### Inventario del Codebase

| Metrica | Valor |
|---------|-------|
| Controllers | 26 |
| Domain Entities | 30 |
| Infrastructure Services | 26 |
| Tablas PostgreSQL | 34 |
| Endpoints API (tenant) | 40+ |
| Endpoints API (backoffice) | 9 |
| Rutas frontend (tenant) | 33 |
| Rutas frontend (backoffice) | 8 |
| Docker services (prod) | 7 |
| MinIO buckets | 4 |

### Lo que ya existe pero el ROADMAP no refleja

| Feature | Archivo | Estado Real |
|---------|---------|-------------|
| Webhook CRUD + deliveries | WebhooksController.cs + WebhookDeliveryService.cs | FUNCIONAL |
| Rate limiting | InMemoryRateLimiter.cs | EXISTE (falta middleware) |
| Audit log backend + UI | AuditLogController.cs + /audit-log/page.tsx | FUNCIONAL |
| Email service | EmailService.cs + EmailLog entity | EXISTE (falta trigger auto) |
| NATS publisher | NatsEventPublisher.cs | FUNCIONAL |
| CPE validation | CpeValidationService.cs | EXISTE (falta frontend) |
| Webhook delivery + retries | WebhookDeliveryService.cs | FUNCIONAL |

---

## Comparativa vs Competencia

Ver archivo: [COMPARATIVA.md](./COMPARATIVA.md)

---

## Los 4 Mega-Sprints

```
MEGA-SPRINT 1  ──→  MEGA-SPRINT 2  ──→  MEGA-SPRINT 3  ──→  MEGA-SPRINT 4
  Motor NATS        SUNAT Real         Backoffice Pro       Developer Platform
  ~2-3 dias         ~3-4 dias          ~2-3 dias            ~3-4 dias
```

**Total estimado: ~10-14 dias de desarrollo**

### Mega-Sprint 1: ver [MEGA-SPRINT-1.md](./MEGA-SPRINT-1.md)
### Mega-Sprint 2: ver [MEGA-SPRINT-2.md](./MEGA-SPRINT-2.md)
### Mega-Sprint 3: ver [MEGA-SPRINT-3.md](./MEGA-SPRINT-3.md)
### Mega-Sprint 4: ver [MEGA-SPRINT-4.md](./MEGA-SPRINT-4.md)

---

## Orden de Ejecucion

```
1. MEGA-SPRINT 1: Motor NATS + Webhooks + Email Auto + Notificaciones
   └─ Sin esto los webhooks y emails nunca se disparan automaticamente
   └─ Es lo que hace que la app "se sienta viva"

2. MEGA-SPRINT 2: SUNAT Produccion Real + Homologacion
   └─ Sin esto NO facturas de verdad
   └─ Certificado digital real + set de pruebas SUNAT

3. MEGA-SPRINT 3: Backoffice Pro
   └─ Para operar como SaaS de verdad
   └─ Impersonar tenants, MRR, suscripciones

4. MEGA-SPRINT 4: Developer Platform
   └─ TU NEGOCIO — vender API + SDK a otros devs
   └─ OpenAPI docs + SDK Node/Python + Sandbox + Portal

5. PRD + TESTING COMPLETO
   └─ Test suite exhaustivo antes de deploy

6. VPS DEPLOY (Batch D)
   └─ Docker + SSL + CI/CD + Monitoring + Backups
```

## Post-Deploy (Fase 3 completa)

| Sprint | Que | Cuando |
|--------|-----|--------|
| Sprint 10 | Agentes IA Fase 1 (Validador, Clasificador, Extractor) | Post-launch |
| Sprint 11 | Agentes IA Fase 2 (Copiloto, Analista, Conciliador) | Post-launch |
| Sprint 12 | Optimizacion + Scale (K8s, multi-region, billing) | Con traccion |
