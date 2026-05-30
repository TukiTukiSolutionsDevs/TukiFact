# TukiFact — antes de producción

> Roadmap completo desde "code-complete" hasta "primer cliente facturando real".
> Marca `[x]` lo cerrado, deja `[ ]` lo pendiente. Las tareas están ordenadas
> por criticidad. Estimaciones en horas de dev senior.

**Última actualización:** 2026-05-29 · sesión nocturna
**Branch:** `main`
**Commits base:** `ca52f72` (5 blockers Fase A) + `753e018` (sitio público + leads)

## Resumen de progreso

| Tier | Hecho | Pendiente | Total |
|---|---|---|---|
| 🔴 Bloqueadores duros | 0 | 6 | 6 |
| 🟡 Importantes | 0 | 8 | 8 |
| 🟢 Escala / venta | 0 | 8 | 8 |
| **Total** | **0** | **22** | **22** |

---

## 🔴 Bloqueadores duros — sin esto no se puede vender

### 1. Voided worker (Comunicación de Baja → SUNAT) — B3

- [ ] **Status:** 🔴 abierto
- **Problema:** `VoidedDocumentsController` devuelve 201 pero **nada llega a SUNAT**. El estado se queda en `pending` para siempre. El cliente paga el plan, pulsa "Anular" en `/documents/[id]`, ve toast verde, pero la baja nunca se materializa.
- **Fix:**
  - Crear `IVoidedDocumentService` + `VoidedDocumentService` con state machine `pending → signing → sent → accepted/rejected`.
  - Worker `VoidedDocumentScheduler : BackgroundService` que:
    1. lee voided pendientes
    2. firma RA XML con cert del tenant
    3. llama `_sunatClient.SendSummaryAsync(ruc, ticketNumber, zip, SunatCredentials, ct)`
    4. poll `GetStatusAsync` con backoff hasta CDR
    5. persiste CDR + flip estado
  - Validar 7-day plazo SUNAT en POST (controller).
  - Type-routing: facturas → RA; boletas → RC (resumen, distinto schema).
  - Idempotency key.
- **Archivos:**
  - `src/TukiFact.Application/Interfaces/IVoidedDocumentService.cs` (nuevo)
  - `src/TukiFact.Infrastructure/Services/VoidedDocumentService.cs` (nuevo)
  - `src/TukiFact.Infrastructure/Services/VoidedDocumentScheduler.cs` (nuevo)
  - `src/TukiFact.Api/Controllers/VoidedDocumentsController.cs` (refactor)
  - Migration: agregar columnas `XmlUrl`, `CdrUrl`, `SunatTicket`, `RetryCount`, `LastError`
- **Esfuerzo:** ~3h

### 2. C7 — materialización atómica + recovery worker

- [ ] **Status:** 🔴 abierto
- **Problema:** En los 4 flujos SUNAT el correlativo se consume (`AddAsync` commit) ANTES de la llamada SOAP. Si el proceso muere entre commit + UpdateAsync, queda doc en `draft` con XML/hash perdido, SUNAT ya recibió XML, y la próxima emisión reutiliza el mismo número → SUNAT 2109 "ya fue presentado".
- **Fix:**
  - Persistir `HashCode` + `Status=Signing` **antes** del SUNAT call (mismo Save).
  - Recovery worker que al startup busca docs en `Signing` > 60s sin update, consulta SUNAT por ticket, reconcilia.
  - State machine explícita con guards.
- **Archivos:** los 4 controllers + DocumentService + nuevo `EmissionRecoveryHostedService`.
- **Esfuerzo:** ~3h

### 3. C6 — Idempotency-Key middleware

- [ ] **Status:** 🔴 abierto
- **Problema:** POST a `/v1/documents`, `/v1/perceptions`, `/v1/retentions`, `/v1/voided-documents` no leen header `Idempotency-Key`. Doble click = doble factura emitida a SUNAT, doble correlativo, dos cobros al cliente.
- **Fix:**
  - Middleware ASP.NET que lee `Idempotency-Key`, hashea body, almacena `(tenant_id, key, request_hash, response_body, response_status, expires_at)` en tabla `idempotency_keys` (TTL 24h).
  - Si key + hash matchean → replay response stored.
  - Si key + hash distinto → 409 Conflict.
- **Archivos:** nuevo middleware + nueva tabla + migration.
- **Esfuerzo:** ~1.5h

### 4. C5 — FluentValidation en 4 flujos SUNAT

- [ ] **Status:** 🔴 abierto
- **Problema:** Ningún controller tiene validators. Inputs basura llegan a SUNAT y rebotan con códigos crípticos. Específicos por flujo: RUC mod-11, regímenes Cat 23, percentajes (3/6/10/15), montos > 0, currency PEN/USD, customer doc type ↔ doc number, series regex, items count > 0.
- **Fix:**
  - `CreateDocumentRequestValidator`, `CreateCreditNoteRequestValidator`, `CreateDebitNoteRequestValidator`
  - `CreatePerceptionRequestValidator`, `CreateRetentionRequestValidator`, `VoidDocumentRequestValidator`
  - Registro en DI + `[ApiController]` auto-validation.
- **Archivos:** `src/TukiFact.Application/Validation/` (carpeta ya existe con `Recurring*` y `DespatchAdvice*`).
- **Esfuerzo:** ~3h

### 5. Per-host routing `tukifact.pe` ↔ `app.tukifact.pe`

- [ ] **Status:** 🔴 abierto
- **Problema:** Hoy `(public)` y `(authenticated)` viven en el mismo Next.js app y comparten el mismo host. En prod necesitamos:
  - `tukifact.pe` → solo grupo `(public)` (marketing)
  - `app.tukifact.pe` → solo `(authenticated)` + `/login`, `/register`, `/welcome`, etc.
- **Fix:**
  - `middleware.ts` en `src/tukifact-web/src/` que inspecciona `host` header:
    - si `host === 'tukifact.pe'` y path empieza con `/dashboard|/documents|...` → 404 o redirect a `app.`
    - si `host === 'app.tukifact.pe'` y path es `/` o `/planes|/funcionalidades|...` → 404 o redirect
  - En dev (`localhost:3000`) deshabilitar el split.
- **Archivos:** `src/tukifact-web/src/middleware.ts` (nuevo).
- **Esfuerzo:** ~1h

### 6. HTTPS + dominio en producción

- [ ] **Status:** 🔴 abierto (infra, decisión usuario)
- **Pasos:** registrar `tukifact.pe`, configurar DNS A/AAAA + CNAME `app`, Let's Encrypt vía Caddy/Traefik/nginx, redirección HTTP→HTTPS, HSTS.
- **Esfuerzo:** ~2h (depende del proveedor de hosting elegido)

---

## 🟡 Importantes — puedes lanzar con caveats, pero molestan

### 7. C2 — Publisher events + email post-aceptación

- [ ] **Status:** 🟡 abierto
- **Problema:** Handlers existen (`NotificationEventHandler`, `GenericWebhookHandler`) suscritos a `document.sent`, `document.accepted`, `perception.created`, `retention.created`, `document.voided`. **Ningún publisher llama estos eventos.** Resultado: el cliente nunca recibe email con PDF+XML, los webhooks que el tenant configuró no disparan.
- **Fix:** inyectar `IEventPublisher` en `DocumentService`, `PerceptionsController`, `RetentionsController`, `VoidedDocumentService`. Publicar `xxx.accepted` después de `Update` cuando `Status = Accepted`.
- **Esfuerzo:** ~2h

### 8. C3 — USD FX persistence al emitir

- [ ] **Status:** 🟡 abierto
- **Problema:** `Document.ExchangeRate` existe en el entity pero nunca se llena. En reconciliación mensual PLE 14.1, el reporte usa el rate ACTUAL → totales PEN incorrectos.
- **Fix:** en `DocumentService.BuildDocument`, si `Currency == "USD"` fetch SBS rate de `ExchangeRateService` para `IssueDate`, persistir en `document.ExchangeRate` + `ExchangeRateDate`. Fail-fast si SBS unreachable.
- **Esfuerzo:** ~1h

### 9. C9 — advisory_xact_lock en GetNextCorrelativeAsync

- [ ] **Status:** 🟡 abierto
- **Problema:** Los 4 repos hacen `MAX(Correlative) + 1` sin lock. Dos POST concurrentes contra mismo `(tenant, serie)` → mismo correlativo → unique-index error 500.
- **Fix:** envolver en `pg_advisory_xact_lock(hashtext(tenantId::text || serie))` dentro del Add.
- **Esfuerzo:** ~1h (4 repos paralelos).

### 10. Documents #8 — PDF persistence en MinIO al aceptarse

- [ ] **Status:** 🟡 abierto
- **Problema:** En cada GET `/documents/{id}/pdf` se regenera el PDF en RAM. `Document.PdfUrl` está siempre null. Slow + CPU waste.
- **Fix:** después de `Status = Accepted` en `DocumentService` y `ProcessAndSendDocument`, generar PDF, subir a MinIO, persistir URL. Controller streamea de storage.
- **Esfuerzo:** ~45min

### 11. Fase B — DS pass a 4 pantallas internas

- [ ] **Status:** 🟡 abierto
- **Páginas:** `/webhooks` (326 líneas), `/api-keys` (355), `/exchange-rates` (302), `/settings` (666).
- **Spec:** secciones en `DESIGN.md` líneas 3786 (api-keys), 3884 (webhooks), 2725 (exchange-rates), 4624 (settings).
- **Reutilizar:** 9 primitivas en `src/components/ui/` (Section, PillGroup, StatusBadge, Toolbar, KpiCard, NumericInput, SunatLookup, Timeline, PaginationFooter).
- **Esfuerzo:** ~3h

### 12. Tests mínimos por flujo SUNAT

- [ ] **Status:** 🟡 abierto
- **Cobertura mínima por flujo:** happy path, IDOR cross-tenant, SUNAT reject mapping, idempotency replay.
- **Stack:** xUnit + Testcontainers para Postgres + FakeItEasy + xUnit data attributes.
- **Esfuerzo:** ~4h

### 13. Sentry / observability — logs + alerts

- [ ] **Status:** 🟡 abierto
- **Fix:** Sentry SDK en API + Next, source-maps en build, alertas Slack/email para exceptions críticas (Tenant emit failures > 5/min).
- **Esfuerzo:** ~2h

### 14. Backoffice de leads

- [ ] **Status:** 🟡 abierto
- **Problema:** `POST /v1/leads` persiste OK pero nadie ve los leads.
- **Fix:** página `/backoffice/leads` con tabla (search, filter, status update), patch endpoint `PATCH /v1/leads/{id}` para cambiar status + notas. Notificación email/Slack al crear lead nuevo.
- **Esfuerzo:** ~1.5h

---

## 🟢 Escala / cerrar venta

### 15. Billing real — Stripe / Culqi / Mercado Pago

- [ ] **Status:** 🟢 abierto
- **Hoy:** los planes son cosméticos (no hay cobro automático). Necesitas integrar provider que soporte recurring + 3DS + facturación para Perú. Culqi y Mercado Pago son los principales en Perú.
- **Esfuerzo:** ~8h (integración base + webhook subscription.updated/canceled + UI plan upgrade/downgrade)

### 16. Plan limits enforcement

- [ ] **Status:** 🟢 abierto
- **Hoy:** un cliente del plan Free podría emitir 10,000 docs sin límite.
- **Fix:** middleware o check en `EmitAsync` que cuenta docs del mes vs `plan.maxDocumentsPerMonth`, devuelve 402 Payment Required si excede.
- **Esfuerzo:** ~2h

### 17. Cloudflare Turnstile en /contacto

- [ ] **Status:** 🟢 abierto
- **Problema:** form de leads sin captcha → spam.
- **Fix:** widget Turnstile en frontend + verify server-side antes de persist lead.
- **Esfuerzo:** ~1h

### 18. Plausible analytics

- [ ] **Status:** 🟢 abierto
- **Fix:** script Plausible en `(public)/layout.tsx` con `data-domain="tukifact.pe"`.
- **Esfuerzo:** ~30min

### 19. Backups + restore procedure

- [ ] **Status:** 🟢 abierto
- **Fix:** `pg_dump` nightly a S3/R2 con retención 30 días + procedimiento documentado.
- **Esfuerzo:** ~3h

### 20. CI/CD — GitHub Actions

- [ ] **Status:** 🟢 abierto
- **Fix:** pipeline `test → build → deploy` con caché de dotnet/pnpm.
- **Esfuerzo:** ~4h

### 21. Screenshots reales del portal en landing

- [ ] **Status:** 🟢 abierto
- **Hoy:** `(public)/page.tsx` tiene un "dashboard preview" hardcoded con 3 docs ficticios.
- **Fix:** capturar screenshots reales (con cert + datos beta) y reemplazar como `<Image src="/landing/dashboard-screenshot.png" />`.
- **Esfuerzo:** ~1h

### 22. Privacy + Terms revisión legal

- [ ] **Status:** 🟢 abierto · externa
- **Acción:** enviar `/privacy` y `/terms` actuales a abogado especializado en SaaS Perú para revisión LGPD-PE.

---

## Decisiones de infra pendientes (necesito tu input)

- [ ] **API hosting:** DigitalOcean droplet 4GB (~$24/mes) / AWS Fargate / Railway / Fly.io?
- [ ] **DB:** DigitalOcean managed Postgres (~$15/mes) / Neon / Supabase?
- [ ] **Object storage:** MinIO self-hosted o S3 / R2 / DO Spaces?
- [ ] **NATS:** self-hosted en mismo droplet o Synadia Cloud?
- [ ] **Web:** Vercel (gratis hasta cierto tráfico) o mismo droplet?
- [ ] **Dominio:** ¿ya tienes `tukifact.pe` registrado?
- [ ] **Cert SUNAT:** ¿usaremos Llama.pe gratis (beta) para validación + luego cert producción?

---

## Camino mínimo viable a "primera factura real"

```
día 1 (hoy)  : cert Llama beta + verificar flujo C1 + ataco bloqueadores 🔴
día 2        : termino bloqueadores 🔴 que queden + arranco importantes 🟡
día 3        : Stripe básico + Fase B 2 pantallas críticas
día 4        : Dominio + HTTPS + deploy + smoke beta
día 5        : Primera factura producción contigo de prueba
```

**Esfuerzo total bloqueadores + importantes: ~28h dev**, repartido en ~5 días.

---

## Notas de la sesión 2026-05-29

- Cerrados ya (commit `ca52f72`): C1 SUNAT creds, C4 Lima TZ, C8 IDOR, Docs #9 signing, Webhooks IDOR.
- Sitio público live (commit `753e018`): `/`, `/planes` (real /v1/plans), `/funcionalidades`, `/seguridad`, `/contacto` (POST a /v1/leads verificado).
- Sesión nocturna: ataco bloqueadores duros 🔴 en orden 5 → 3 → 1 → 2 → 4. Ver final del archivo.
