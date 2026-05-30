# TukiFact — antes de producción

> Roadmap completo desde "code-complete" hasta "primer cliente facturando real".
> Marca `[x]` lo cerrado, deja `[ ]` lo pendiente. Las tareas están ordenadas
> por criticidad. Estimaciones en horas de dev senior.

**Última actualización:** 2026-05-30 01:00 · sesión nocturna
**Branch:** `main`
**Commits hasta ahora:**
- `ca52f72` 5 blockers Fase A (SUNAT creds, Lima TZ, IDOR, signing, webhooks IDOR)
- `753e018` sitio público + leads endpoint
- `cf2bb20` per-host routing + Idempotency-Key middleware (#5 + #3)
- `?` validators 4 flujos SUNAT (#4)

## Resumen de progreso

| Tier | Hecho | Pendiente | Total |
|---|---|---|---|
| 🔴 Bloqueadores duros | **3** | 3 | 6 |
| 🟡 Importantes | 0 | 8 | 8 |
| 🟢 Escala / venta | 0 | 8 | 8 |
| **Total** | **3** | **19** | **22** |

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

### 3. C6 — Idempotency-Key middleware ✅ CERRADO

- [x] **Status:** ✅ cerrado en sesión nocturna 2026-05-29
- **Commit:** `cf2bb20`
- **Implementado:**
  - Entity `IdempotencyKey` + tabla `idempotency_keys` (TTL 24h, índice único en `(TenantId, Key)`)
  - `IdempotencyMiddleware` ([file](src/TukiFact.Infrastructure/Middleware/IdempotencyMiddleware.cs:1)) buffera request body + hash SHA-256
  - Replay con header `X-Idempotent-Replay: true` cuando key + hash matchean
  - 409 Conflict cuando key reusada con body distinto
  - Solo se activa en POST a los 6 endpoints de emisión
  - Tenant scope vía claim `tenant_id` del JWT
  - Migration `20260530004045_IdempotencyKeys_AddTable` aplicada
- **Esfuerzo real:** ~1h

### 4. C5 — Validators en 4 flujos SUNAT ✅ CERRADO

- [x] **Status:** ✅ cerrado en sesión nocturna 2026-05-29
- **Implementado (hand-rolled siguiendo patrón GRE/Recurring):**
  - [`SunatIdentity.cs`](src/TukiFact.Application/Validation/SunatIdentity.cs) — mod-11 RUC + DNI shape + Catálogos 02/06/07
  - [`DocumentValidator.cs`](src/TukiFact.Application/Validation/DocumentValidator.cs) — `Validate()` para Factura/Boleta + `ValidateCreditNote()` + `ValidateDebitNote()`. Reglas: type, serie regex, currency, customer (RUC obligatorio para FB), items ≥1/≤5000 con qty/price/IGV/unit measure.
  - [`PerceptionValidator.cs`](src/TukiFact.Application/Validation/PerceptionValidator.cs) — régimen ↔ % (01/02/03 → 2/1/0.5), serie P###, currency PEN, customer RUC, refs ≥1/≤100 con FX si USD.
  - [`RetentionValidator.cs`](src/TukiFact.Application/Validation/RetentionValidator.cs) — régimen 01→3%, 02→6%, serie R###, currency PEN, supplier RUC, refs validation.
  - [`VoidDocumentValidator.cs`](src/TukiFact.Application/Validation/VoidDocumentValidator.cs) — DocumentId + voidReason 5..100.
  - Wire en 4 controllers (Documents.Emit/EmitCreditNote/EmitDebitNote, Perceptions.Create, Retentions.Create, VoidedDocuments.VoidDocument).
  - Respuesta uniforme: `400 { error: "Datos inválidos…", details: [\"…\", \"…\"] }` con TODOS los errores al toque (no fix→retry→next).
- **Esfuerzo real:** ~1.5h

### 5. Per-host routing `tukifact.com.pe` ↔ `app.tukifact.com.pe` ✅ CERRADO

- [x] **Status:** ✅ cerrado en sesión nocturna 2026-05-29
- **Commit:** `cf2bb20`
- **Implementado:** [`src/tukifact-web/src/middleware.ts`](src/tukifact-web/src/middleware.ts:1) inspecciona el `host` header del request:
  - `localhost`/`127.0.0.1`/`*.local`/`*.test` → pass through (dev sin split).
  - `app.tukifact.com.pe` + path `/` → redirect 307 a `/dashboard`.
  - `app.tukifact.com.pe` + path marketing (`/planes`, `/funcionalidades`, `/seguridad`, `/contacto`) → redirect 301 al host root.
  - `tukifact.com.pe` + path portal/auth → redirect 301 a `app.{domain}`.
  - `/privacy` y `/terms` permitidos en ambos hosts.
  - Excluye sitemap/robots/static via matcher.
  - **Nota dominio:** el middleware deriva el root domain dinámicamente (`host.replace(/^app\./, '')`), así que funciona con `tukifact.com.pe`, `tukifact.pe`, o cualquier futuro dominio sin tocar código.
- **Esfuerzo real:** ~30min

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
- [x] **Dominio:** `tukifact.com.pe` ya está registrado (confirmado por usuario 2026-05-30).
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

## Notas de la sesión 2026-05-29 (sesión completa)

### Día (commits `ca52f72` + `753e018`)
- Fase A — auditoría production-readiness de 4 flujos SUNAT (Perceptions, Retentions, Voided, DocumentService): 4 explore agents en paralelo, veredicto 🔴 BLOCKED en los 4 con ~30 blockers consolidados.
- Cerrados 5 blockers críticos compartidos: C1 (per-tenant SUNAT creds), C4 (Lima TZ), C8 (IDOR cross-tenant en 4 repos), Documents #9 (silent signing failure → unsigned XML a SUNAT), Webhooks IDOR (PUT/DELETE/GetDeliveries).
- Fase C — sitio público completo desde cero: `/`, `/planes` (fetch real `/v1/plans`), `/funcionalidades`, `/seguridad`, `/contacto` con form. Backend: Lead entity + endpoint `POST /v1/leads`. sitemap.ts + robots.ts. Verificado: 1 lead persistido en DB.

### Noche (commits `cf2bb20` + validators)
- ✅ #5 Per-host routing — `src/tukifact-web/src/middleware.ts` con split `tukifact.pe` ↔ `app.tukifact.pe`, deshabilitado en localhost para dev. 30min.
- ✅ #3 C6 Idempotency-Key middleware — entity + tabla + middleware con SHA-256 body hash, replay 24h, 409 en hash conflict. Wire global en pipeline (después de Audit, antes de MapControllers). Migration aplicada. ~1h.
- ✅ #4 C5 Validators — 5 archivos en `Application/Validation/` (Document/Perception/Retention/VoidDocument + SunatIdentity helper). Wire en 4 controllers. ~1.5h.
- 🔴 #1 Voided worker — NO INICIADO. Scope estimado 3-4h por necesitar SUNAT Summary XML builder desde cero. Defer a próxima sesión con dedicación full.
- 🔴 #2 C7 atomic materialization — NO INICIADO. Scope 3h. Defer.
- 🔴 #6 HTTPS + dominio — infra, requires hosting decisions del usuario.

### Estado al cierre nocturno
- **3 de 6 🔴 bloqueadores duros cerrados** (50%).
- Lo restante (#1, #2) son los items con más código nuevo (worker + recovery). Mejor abordarlos con full session y no dejar a medias.
- Sesión siguiente arrancar por **#1 Voided worker** (impacto usuario crítico) y luego **#2 atomic mat**.
