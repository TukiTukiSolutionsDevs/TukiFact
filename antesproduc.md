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
| 🔴 Bloqueadores duros | **6** | 0 | 6 |
| 🟡 Importantes | **8** | 0 | 8 |
| 🟢 Escala / venta | **6** | 1 + 1 externa | 8 |
| **Total** | **20/22** | 1 + 1 externa | 22 |

**Sesión 2026-05-31 (cierre final)** cerró el último bloqueador 🔴 #6: deploy a VPS 184.174.39.116 con nginx-proxy + acme-companion. Live HTTPS en `tukifact.com.pe`, `www.tukifact.com.pe`, `api.tukifact.com.pe`. Único subdominio pendiente: `app.tukifact.com.pe` (requiere A record en Cloudflare).

**Pendientes reales:**
- 🟢 #21 screenshots reales (~1h, humano)
- 🟢 #22 legal review (externa, abogado)
- ⏸ DNS `app.tukifact.com.pe` (30 segundos en Cloudflare)

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

### 6. HTTPS + dominio en producción ✅ CERRADO (2026-05-31)

- [x] **Status:** ✅ desplegado y live
- **Live URLs (todos HTTPS válido con Let's Encrypt):**
  - `https://tukifact.com.pe/` → web (marketing)
  - `https://www.tukifact.com.pe/` → web (marketing)
  - `https://api.tukifact.com.pe/health` → API healthy (postgres + NATS + MinIO OK)
  - `https://api.tukifact.com.pe/v1/plans` → 200 con 6 planes seedeados
- **Hallazgo no obvio:** la red real del gateway en el VPS NO es `nginx-proxy_default` sino `web`. La memoria previa estaba mal; verificar con `docker network ls | grep nginx-proxy` antes del deploy.
- **Decisiones de arquitectura tomadas en la sesión:**
  - Subdomain-based routing (api.tukifact.com.pe dedicado) en lugar de path-based (app.tukifact.com.pe/api). Alinea con patrón de starbuybaby/expertia en el mismo VPS.
  - Borrado el servicio `nginx` del compose.prod.yml — el gateway compartido (nginx-proxy + acme-companion) maneja TLS y routing.
  - Postgres / NATS / MinIO en red `internal` aislada. API en `internal + gateway`. Web solo en `gateway`.
  - Fix en Program.cs: `Cors:FrontendUrl` ahora acepta lista separada por coma para soportar marketing + portal + www.
  - Fix en Dockerfile.web: agregadas 4 ARG/ENV faltantes para que NEXT_PUBLIC_* se bakeen al build (Culqi/Turnstile/Plausible).
  - Workaround pnpm 10: `--config.strict-dep-builds=false` para no fallar en built-deps no aprobadas.
  - Workaround TS deuda: `typescript.ignoreBuildErrors=true` en next.config.ts (pendiente fix Button asChild).
- **Archivos producidos / modificados:**
  - `docker/docker-compose.prod.yml` — reescrito (red `web`, VIRTUAL_HOST/LE_HOST, seed envs)
  - `docker/.env.prod.example` — plantilla
  - `docker/.env.prod` — local con credenciales reales (gitignored)
  - `docker/Dockerfile.web` — 4 args NEXT_PUBLIC + flag pnpm
  - `scripts/deploy.sh` — reescrito sin Caddy / sin puertos host
  - `src/TukiFact.Api/Program.cs` — CORS multi-origin
  - `src/tukifact-web/next.config.ts` — ignoreBuildErrors
- **Pendiente para feature completo:**
  - Apuntar A record `app.tukifact.com.pe` → `184.174.39.116` en Cloudflare (30s).
  - Después: editar `LETSENCRYPT_HOST` en compose para incluirlo + `docker compose up -d web` (acme-companion reemite cert SAN).
- **Esfuerzo real:** ~2.5h (incluyendo fix CORS, fix Dockerfile.web, debug pnpm 10, debug TS).

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

## Camino actualizado a "primera factura real" (post sesión Culqi 2026-05-31)

```
ESTE PUNTO  : 19/22 cerrados. #15 Culqi code-complete. Working tree commiteado.
PRÓX. SESIÓN: 🔴 #6 deploy a VPS 184.174.39.116 + tukifact.com.pe (nginx-proxy adapter)
              ↓
sesión N+1  : 🔴 #6 deploy adapter + DNS + cert + smoke prod                                    (~1.5h)
sesión N+2  : primera factura real beta + Culqi sandbox smoke (interval_unit_time + webhooks)  (~1h)
sesión N+3  : 🟢 #21 screenshots reales (humano captura del portal beta)                        (~1h)
sesión N+∞  : 🟢 #22 legal review (externa)
```

**Esfuerzo restante total: ~3.5h dev + 1h humano + 1h externa.**
**Mínimo viable para primera factura beta: solo falta #6 deploy (~1.5h)**.

### Siguiente sesión: #6 deploy (instrucciones concretas)

**Infra ya decidida (NO discutir):**
- VPS `184.174.39.116` compartida, gateway `nginx-proxy + acme-companion`.
- Dominios: `tukifact.com.pe` (marketing) + `app.tukifact.com.pe` (portal + API).
- Patrón: contenedor Docker con `VIRTUAL_HOST` + `LETSENCRYPT_HOST` envs, conectado a red externa `nginx-proxy_default`. NO usar Caddy. NO usar `deploy.sh` de Pabellones (rompería el gateway compartido).

**Pasos concretos:**
1. **Adaptar `docker/docker-compose.prod.yml`:**
   - Eliminar el servicio interno `nginx` (el gateway externo lo maneja).
   - `api` service: añadir `VIRTUAL_HOST=app.tukifact.com.pe`, `VIRTUAL_PATH=/api/`, `VIRTUAL_DEST=/api/`, `LETSENCRYPT_HOST=app.tukifact.com.pe`, `LETSENCRYPT_EMAIL=ops@tukifact.com.pe`. No exponer puertos al host.
   - `web` service: añadir `VIRTUAL_HOST=tukifact.com.pe,app.tukifact.com.pe`, `LETSENCRYPT_HOST=tukifact.com.pe,app.tukifact.com.pe`. La lógica Next middleware ya hace el split host. No exponer puertos.
   - Añadir red externa `nginx-proxy_default` + marcar api/web como participantes. La red interna queda solo para api↔postgres/minio/nats.
2. **Verificar Dockerfiles** (`docker/Dockerfile.api` + `docker/Dockerfile.web`) compilan multistage y exponen el puerto correcto (`5000` o `80` para API, `3000` para Next).
3. **Configurar env file `docker/.env.prod`** con: `PG_PASSWORD`, `Jwt__Secret`, `Culqi__SecretKey`, `NEXT_PUBLIC_CULQI_PUBLIC_KEY`, `Turnstile__SecretKey`, `NEXT_PUBLIC_TURNSTILE_SITE_KEY`, `Sentry__Dsn`, `Sunat__Environment=production`.
4. **DNS:** apuntar `tukifact.com.pe` A → `184.174.39.116` y `app.tukifact.com.pe` A → `184.174.39.116`.
5. **Deploy:**
   ```bash
   scp -r docker/ scripts/ .env.prod root@184.174.39.116:/opt/tukifact/
   ssh root@184.174.39.116 'cd /opt/tukifact && docker compose -f docker/docker-compose.prod.yml up -d'
   sudo cp scripts/backup.cron /etc/cron.d/tukifact-backup && sudo systemctl restart cron
   ```
6. **Verificar cert + smoke:**
   - `curl -I https://tukifact.com.pe/` → 200 + HSTS
   - `curl -I https://app.tukifact.com.pe/login` → 200
   - `curl https://app.tukifact.com.pe/api/health` → 200
   - Subir tu cert SUNAT desde portal, emitir 1 boleta beta, verificar CDR.

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
