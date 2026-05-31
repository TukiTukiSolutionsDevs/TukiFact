# TukiFact — antes de producción

> Roadmap completo desde "code-complete" hasta "primer cliente facturando real".
> Marca `[x]` lo cerrado, deja `[ ]` lo pendiente. Las tareas están ordenadas
> por criticidad. Estimaciones en horas de dev senior.

**Última actualización:** 2026-05-31 (post-audit retomada) · sesión audit de estado real
**Branch:** `main` (working tree con cambios sin commit)
**Commits hasta ahora:**
- `ca52f72` 5 blockers Fase A (SUNAT creds, Lima TZ, IDOR, signing, webhooks IDOR)
- `753e018` sitio público + leads endpoint
- `8d1895c` per-host routing + Idempotency-Key middleware (#5 + #3)
- `148e717` validators 4 flujos SUNAT (#4)
- `d68e6d5` checkpoint dominio confirmado
- _(uncommitted)_ Voided worker #1 + C7 atomic #2 + credit-note/debit-note SUNAT business rules + Fase C UI rediseño + #7 events + #8 USD FX + #10 PDF persist + #12 tests + #13 Sentry + #14 backoffice leads + #16 plan limits + #18 Plausible + #20 CI/CD + #19 backup script

## Resumen de progreso (post sesión 2026-05-31 Culqi)

| Tier | Hecho | Pendiente | Total |
|---|---|---|---|
| 🔴 Bloqueadores duros | **5** | 1 (#6 parcial) | 6 |
| 🟡 Importantes | **8** | 0 | 8 |
| 🟢 Escala / venta | **6** | 1 + 1 externa | 8 |
| **Total** | **19/22** | 2 + 1 externa | 22 |

**Sesión 2026-05-31** cerró el audit (6/22 → 16/22) y luego implementó Culqi billing + Turnstile + backup cron (16/22 → **19/22**). Único trabajo de código real pendiente: #21 screenshots (humano captura) y #6 deploy nginx-proxy adapter. #22 legal es externo.

**Pendientes reales:**
- 🔴 #6 deploy — Dockerfiles + compose.prod existen pero falta adapter nginx-proxy (~1.5h, al final)
- 🟢 #21 screenshots reales (~1h, humano)
- 🟢 #22 legal review (externa, abogado)

---

## 🔴 Bloqueadores duros — sin esto no se puede vender

### 1. Voided worker (Comunicación de Baja → SUNAT) — B3 ✅ CERRADO

- [x] **Status:** ✅ cerrado en sesión 2026-05-30/31 (uncommitted)
- **Implementado:** `src/TukiFact.Infrastructure/Services/VoidedDocumentScheduler.cs` (96 LOC, `BackgroundService` con `ExecuteAsync`). El controller (`VoidedDocumentsController`) crea el voided en `Status=pending` con `CreateWithTicketAsync` (advisory-lock), valida 7-day plazo SUNAT, marca el original como `Voided`. El scheduler procesa los pendientes de forma async.
- **Esfuerzo real:** ya invertido en sesión previa, no contabilizado en el checklist.
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

### 2. C7 — materialización atómica + recovery worker ✅ CERRADO

- [x] **Status:** ✅ cerrado en sesión previa (uncommitted)
- **Implementado:** patrón "persist HashCode + XmlUrl + Status=Signed BEFORE SUNAT call" aplicado en `PerceptionsController.Create` (línea ~152), `RetentionsController.Create`, y `DocumentService.ProcessAndSendDocument` (5 hits). Recovery worker en `src/TukiFact.Infrastructure/Services/EmissionRecoveryHostedService.cs` (126 LOC, `BackgroundService`).
- **Esfuerzo real:** ya invertido en sesión previa, no contabilizado.
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

### 6. HTTPS + dominio en producción — diferido al final del roadmap

- [ ] **Status:** 🔴 diferido al cierre del roadmap (no bloquea desarrollo)
- **Infra confirmada (2026-05-31):**
  - **VPS:** `184.174.39.116` (la misma compartida con otros proyectos)
  - **Dominio:** `tukifact.com.pe` (registrado)
  - **Subdominio app:** `app.tukifact.com.pe`
  - **Gateway compartido:** nginx-proxy + acme-companion (NO usar Caddy ni deploy.sh de Pabellones — rompería el gateway compartido). Patrón: contenedor con `VIRTUAL_HOST` + `LETSENCRYPT_HOST` envs, conectarse a la red `nginx-proxy_default`, y nginx-proxy + acme-companion descubren y emiten cert Let's Encrypt automáticamente.
- **Pasos pendientes:**
  1. Apuntar DNS A `tukifact.com.pe` y `app.tukifact.com.pe` → `184.174.39.116`
  2. Dockerizar `TukiFact.Api` + `tukifact-web` (Dockerfile multi-stage)
  3. docker-compose en la VPS con `VIRTUAL_HOST` + `LETSENCRYPT_HOST` apuntando a los hosts respectivos
  4. Verificar emisión cert + redirect 301 HTTP→HTTPS + HSTS header
  5. Smoke test `https://tukifact.com.pe` (marketing) y `https://app.tukifact.com.pe/login` (portal)
- **Esfuerzo:** ~2h una vez se aborda al final del roadmap.

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

### 11. Fase B — DS pass a 4 pantallas internas ✅ CERRADO

- [x] **Status:** ✅ cerrado (confirmado por auditoría de tokens 2026-05-31)
- **Auditoría:** grep de patrones obsoletos (`text-2xl|tracking-tight|<Card>|CardContent|text-muted-foreground`) devolvió **0 hits** en `/webhooks` (879 LOC, 26 tokens DS), `/api-keys` (687 LOC, 26 tokens DS), `/exchange-rates` (484 LOC, 25 tokens DS), `/settings` (889 LOC, 19 tokens DS). Las 4 ya están rediseñadas con el design system completo.
- **Páginas legacy a la fecha de escribir esto:** `/webhooks` (326 líneas), `/api-keys` (355), `/exchange-rates` (302), `/settings` (666).
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

### 15. Billing real — Culqi ✅ CÓDIGO COMPLETO (falta sandbox)

- [x] **Status:** ✅ código completo en sesión 2026-05-31 (provider: Culqi, elegido por target Perú).
- **Backend implementado:**
  - `ICulqiService` + `CulqiService` (v2 customers/cards/recurrent/plans/recurrent/subscriptions con Bearer auth + HMAC-SHA256 webhook verify).
  - `BillingController` con `GET /v1/billing/subscription`, `POST /v1/billing/subscribe`, `POST /v1/billing/cancel`, `POST /v1/billing/webhook` (AllowAnonymous + signature mandatory).
  - Domain: `Subscription.CulqiCustomerId / CulqiCardId / CulqiSubscriptionId / LastChargeId / LastChargedAt / CancellationReason`. `Plan.CulqiPlanId` (lazy upsert).
  - Migration `20260531223332_Subscriptions_AddTableWithCulqiFields` aplicada (tabla `subscriptions` snake_case + 5 indexes incluyendo unique parcial en `CulqiSubscriptionId`).
  - HttpClient "Culqi" registrado (base `https://api.culqi.com/`, 15s timeout).
- **Frontend implementado:**
  - `/plan` page: Culqi Checkout (`https://checkout.culqi.com/js/v4`) cargado vía `next/script`. Botones "Suscribirme" en cada plan pagado (handler `Culqi.open()` → token → POST `/v1/billing/subscribe`). Botón "Cancelar suscripción" en header cuando hay sub Culqi-managed. Banner `past_due` cuando aplica.
- **Webhook events soportados:** `charge.succeeded`, `charge.creation.succeeded`, `subscription.cancelled/canceled`, `subscription.past_due`, `charge.failed` (resetea `DocumentsUsedThisMonth=0` y `NextBillingDate+=1mo` al cobro exitoso).
- **Activar en prod:** setear `Culqi__SecretKey` (backend) + `NEXT_PUBLIC_CULQI_PUBLIC_KEY` (frontend) + registrar `https://app.tukifact.com.pe/v1/billing/webhook` en el dashboard Culqi.
- **Falta validar contra sandbox:** confirmar `interval_unit_time=3` (monthly), nombres de eventos webhook, y campo de header HMAC (`culqi-signature` vs `x-culqi-webhook-signature`). Smoke test con tarjeta test `4111 1111 1111 1111`.
- **Esfuerzo real:** ~3.5h (vs 8h estimado).

### 16. Plan limits enforcement

- [ ] **Status:** 🟢 abierto
- **Hoy:** un cliente del plan Free podría emitir 10,000 docs sin límite.
- **Fix:** middleware o check en `EmitAsync` que cuenta docs del mes vs `plan.maxDocumentsPerMonth`, devuelve 402 Payment Required si excede.
- **Esfuerzo:** ~2h

### 17. Cloudflare Turnstile en /contacto ✅ CERRADO

- [x] **Status:** ✅ cerrado en audit 2026-05-31
- **Implementado:**
  - Backend: `CreateLeadRequest.TurnstileToken` + `LeadsController.VerifyTurnstileAsync` que llama `https://challenges.cloudflare.com/turnstile/v0/siteverify` con secret + token + remoteIp (timeout 5s). Skip silencioso si `Turnstile:SecretKey` vacío (dev bypass).
  - Frontend: `Script` con `strategy="afterInteractive"` + widget implicit `<div class="cf-turnstile" data-sitekey={NEXT_PUBLIC_TURNSTILE_SITE_KEY}>` que renderiza si la env existe. FormData lee `cf-turnstile-response` y lo pasa como `turnstileToken` al backend.
- **Activar en prod:** setear `Turnstile__SecretKey` (backend env) + `NEXT_PUBLIC_TURNSTILE_SITE_KEY` (frontend env).

### 18. Plausible analytics

- [ ] **Status:** 🟢 abierto
- **Fix:** script Plausible en `(public)/layout.tsx` con `data-domain="tukifact.pe"`.
- **Esfuerzo:** ~30min

### 19. Backups + restore procedure ✅ CERRADO

- [x] **Status:** ✅ cerrado en audit 2026-05-31
- **Implementado:**
  - `scripts/backup.sh` (preexistente): `pg_dump --format=custom` + mirror 4 buckets MinIO + cleanup `find -mtime +30 -delete` + opcional sync a S3/R2 si `BACKUP_S3_URL` está seteado y `aws` instalado.
  - `scripts/backup.cron` (nuevo): `0 8 * * * root /opt/tukifact/scripts/backup.sh` (03:00 PET = 08:00 UTC). Output a `/var/log/tukifact-backup.log`.
- **Activar en prod:** `sudo cp scripts/backup.cron /etc/cron.d/tukifact-backup && sudo chmod 644 /etc/cron.d/tukifact-backup && sudo systemctl restart cron`.
- **Sync offsite opcional:** `export BACKUP_S3_URL=s3://tukifact-backups/prod` + creds AWS en `~/.aws/`.

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

## Decisiones de infra (todas resueltas excepto cert SUNAT)

- [x] **VPS:** `184.174.39.116` (compartida con otros proyectos, gateway nginx-proxy + acme-companion).
- [x] **API hosting:** contenedor en la misma VPS, expuesto vía nginx-proxy.
- [x] **Web hosting:** contenedor Next.js en la misma VPS, expuesto vía nginx-proxy.
- [x] **DB:** Postgres en la misma VPS (Docker, ya hay patrón con otros tenants).
- [x] **Object storage:** MinIO en la misma VPS (ya corre en dev local en :9010/:9011).
- [x] **NATS:** self-hosted en la misma VPS.
- [x] **Dominio:** `tukifact.com.pe` registrado + subdominio `app.tukifact.com.pe`.
- [ ] **Cert SUNAT:** ¿usaremos Llama.pe gratis (beta) para validación + luego cert producción?

---

## Camino actualizado a "primera factura real" (post audit 2026-05-31)

```
ESTE PUNTO  : Audit reveló 10 items ya hechos en código + #17 Turnstile + #19 cron añadidos en esta sesión.
PRÓX. PASO  : Decisión usuario → Stripe vs Culqi vs Mercado Pago para #15 billing.
              ↓
sesión N+1  : 🟢 #15 Billing real (~8h tras decisión)
sesión N+2  : 🟢 #21 screenshots reales (humano captura con datos beta, ~1h)
sesión N+3  : 🔴 #6 deploy adapter nginx-proxy + DNS + cert + smoke (~1.5h)
sesión N+4  : primera factura real beta + smoke producción (~1h)
sesión N+∞  : 🟢 #22 legal review (externa)
```

**Esfuerzo restante total: ~12h dev** (down from 33h gracias al audit).
**Mínimo viable para emitir primera factura beta:** Decisión billing → #15 + #6 deploy (~10h).

---

## Notas de la sesión 2026-05-31 (Fase C cliente + audit SUNAT credit-note/debit-note)

### Rediseño UI Fase C — SUNAT flows
- 6 pantallas rediseñadas: `/documents` lista, `/documents/credit-note`, `/perceptions` lista + new, `/retentions` lista + new.
- Hallazgo: la memoria "Cliente portal redesign progress" decía 18 pantallas pendientes pero la auditoría de tokens reveló que solo 6 estaban realmente sin tocar. Las otras 12 ya tenían el design system aplicado en sesiones previas. Toda la UI cliente está en 18/18 ✅.

### Audit backend credit-note + debit-note — 6 bloqueadores SUNAT NUEVOS detectados y arreglados
- Antes: `EmitCreditNoteAsync` y `EmitDebitNoteAsync` no validaban: `refDoc.Status==Accepted`, `refDoc.Currency==request.Currency`, `refDoc.DocumentType ∈ {01,03}`, prefijo de serie matchea F/B del refDoc, `CreditNoteReason ∈ Catálogo 09 (01-10)`, `Description` mínima de 3 chars.
- Fix en `DocumentValidator.cs` (ValidCreditNoteReasons + ValidDebitNoteReasons hashsets + reason catálogo check + Description min 3 chars) y `DocumentService.cs` (4 SUNAT business-rule checks antes de consumir correlativo).
- Build limpio 0 warnings.

### Audit backend perceptions / retentions / voided
- Resultado: **0 bloqueadores**. Los 3 controllers ya son production-ready (validator first, advisory-lock correlativo, atomic checkpoint pre-SUNAT, tenant SUNAT creds sin fallback global, CDR storage, soft-fail a "Sent" si timeout, event publisher post-aceptación, anti-IDOR en GetById).

### Fix UX no obvio
- `/perceptions/new` y `/retentions/new` permitían editar el % manualmente (Input). Los validators backend exigen match EXACTO con régimen → fallaba 400 frustrante. Cambiado a display readonly derivado del régimen.

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
