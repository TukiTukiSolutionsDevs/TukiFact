# Culqi API v2 — Referencia rápida (suscripciones recurrentes)

> Skill interno de TukiFact. Toda la info viene de los docs oficiales (citados al pie de cada bloque).
> Fecha de captura: 2026-06-23. Re-verificar antes de cambios mayores: estos docs cambian sin aviso.

## ✅ Status (2026-06-23)

End-to-end flow **validado en producción sandbox** (`pk_test_dYUtcOLqXDWhHdoB`) con tarjeta Visa `4111-1111-1111-1111`. Las 3 discrepancias del §0 quedaron resueltas en `CulqiService.cs` por commit `631c0a5`:

- Path POST plan/sub → **se usa `/create`** suffix
- Campo currency en plan → **`currency` (no `currency_code`)**
- `initial_cycles` → **enviado como objeto con `count=0, has_initial_charge=false, amount=0, interval_unit_time=3`**
- `phone_number` + `address` sanitizados (defensivos cuando el tenant no los tiene completos)
- Fallback idempotente para customer-already-registered
- `tyc: true` siempre al crear suscripción

Sub de prueba persistida: `sxn_test_JOVlJgBKUzBCSWDA` (customer `cus_test_rAnQWJSgIjImGVro`).

## 0. ⚠ Discrepancias conocidas (leer antes de tocar `CulqiService`)

Las fuentes oficiales se contradicen entre sí en 3 puntos sensibles. Marcado con ⚠ en cada sección. Resumen:

| Tema | Fuente A | Fuente B | Qué hacer |
|---|---|---|---|
| Path POST plan | Redoc apidocs muestra botón **`POST /v2/recurrent/plans/create`** ([apidocs.culqi.com #tag/Planes/operation/crear-plan](https://apidocs.culqi.com/#tag/Planes/operation/crear-plan)) | SDK .NET oficial postea a `/v2/plans` (sin `/recurrent`, sin `/create`) ([culqi-net README](https://github.com/culqi/culqi-net)) | Estamos posteando a `/v2/recurrent/plans` — confirmar contra sandbox cuando se pruebe live |
| Path POST suscripción | Redoc botón **`POST /v2/recurrent/subscriptions/create`** | SDK postea a `/v2/subscriptions` | Mismo flag — probar en sandbox antes de pasar a live |
| Campo currency en plan | docs.culqi.com guide: `"currency": "PEN"` ([Planes guide](https://docs.culqi.com/es/documentacion/pagos-online/recurrencia/suscripciones/plan)) | SDK .NET README: `"currency_code": "PEN"` ([culqi-net README](https://github.com/culqi/culqi-net)) | **Usar `currency`** (la guía oficial y el Redoc coinciden — el SDK README está desactualizado) |
| Schema plan (general) | Redoc/guía: `name`, `short_name`, `description`, `currency`, `interval_unit_time`, `interval_count`, `initial_cycles` (REQ) | SDK README: `name`, `amount`, `currency_code`, `interval` ("dias"), `interval_count`, `limit`, `trial_days` | **La guía/Redoc es la verdad actual.** SDK README es la API vieja. Nuestro `CulqiService` está a mitad de camino — ver §6. |

---

## 1. TL;DR (5 líneas)

- **Culqi v2** es un PSP peruano (parte de Credicorp). Soporta cargo único, one-click, suscripciones recurrentes, órdenes (PagoEfectivo, billeteras, Cuotéalo) y devoluciones.
- **Base URL:** `https://api.culqi.com/v2` (y `https://secure.culqi.com/v2` cuando se envía payload encriptado AES/RSA). Todas las peticiones requieren HTTPS.
- **Llaves:** `pk_test_*` + `sk_test_*` para sandbox (panel integración); `pk_live_*` + `sk_live_*` para producción (panel principal). Las cuentas y datos son separadas — no se heredan.
- **Auth:** `Authorization: Bearer <secret_key>` para todo lo backend. La `pk_*` se usa solo desde el navegador (Culqi.js / Checkout) para crear tokens.
- **Flujo recurrente:** `Culqi.js token` → `POST /v2/customers` → `POST /v2/cards` (con `token_id + customer_id`) → `POST /v2/recurrent/plans` → `POST /v2/recurrent/subscriptions` (con `card_id + plan_id`).

Fuentes: [apidocs.culqi.com/#section/Introduccion](https://apidocs.culqi.com/#section/Introduccion), [docs.culqi.com Llaves](https://docs.culqi.com/es/documentacion/pagos-online/llaves)

---

## 2. Autenticación

| Endpoint | Llave a usar | Header |
|---|---|---|
| `POST /v2/tokens` (frontend, vía Culqi.js) | `pk_test_*` / `pk_live_*` | `Authorization: Bearer pk_...` |
| `POST /v2/tokens/yape` | `pk_*` | idem |
| **Todo lo demás backend** (customers, cards, charges, plans, subscriptions, refunds, orders, events) | `sk_test_*` / `sk_live_*` | `Authorization: Bearer sk_...` |

Reglas:
- `Content-Type: application/json` siempre que haya body.
- Las llaves `sk_*` **NUNCA** deben salir del backend.
- Si se usa encriptación AES/RSA, además se envía `x-culqi-rsa-id: <uuid>` y el body queda `{ encrypted_data, encrypted_key, encrypted_iv }`. Para TukiFact no aplica hoy (no estamos encriptando payloads).
- HTTP plano falla en producción. Sin auth válida → 401.

Fuente: [apidocs.culqi.com/#section/Autenticacion](https://apidocs.culqi.com/#section/Autenticacion), [apidocs.culqi.com/#section/Encriptacion-AESRSA](https://apidocs.culqi.com/#section/Encriptacion-AESRSA)

---

## 3. Tokenización en el frontend (Culqi.js v4 / Checkout)

**Recomendado para SaaS web:** usar Culqi Checkout (formulario hosteado que se abre en modal o embebido). El que estamos usando hoy en `src/tukifact-web/src/app/(authenticated)/plan/page.tsx`.

Script CDN:

```html
<script src="https://js.culqi.com/checkout-js"></script>
```

> Nota: existe también `https://checkout.culqi.com/js/v4` (Checkout v4 más antiguo). El brief mencionaba ese URL — los docs oficiales actuales referencian `https://js.culqi.com/checkout-js` para el Custom Checkout. Verificar cuál tenemos en el `<head>` antes de actualizar.

Snippet mínimo para tokenizar (solo tarjeta):

```html
<script>
  const settings = {
    title: "TukiFact",
    currency: "PEN",
    amount: 3500,           // céntimos (S/35.00)
    // order: "ord_live_xxx" // SOLO si vas a habilitar PagoEfectivo/Yape/Cuotéalo
  };
  const client = { email: "user@example.com" };
  const options = {
    lang: "es",
    installments: false,
    modal: true,
    paymentMethods: { tarjeta: true, yape: false, billetera: false, bancaMovil: false, agente: false, cuotealo: false },
  };
  const appearance = { theme: "default" };

  const Culqi = new CulqiCheckout("pk_test_dYUtcOLqXDWhHdoB", { settings, client, options, appearance });

  Culqi.culqi = function handleCulqiAction() {
    if (Culqi.token) {
      // POST a nuestro backend con Culqi.token.id (string "tkn_test_xxx")
      fetch("/v1/billing/subscribe", { method: "POST", body: JSON.stringify({ token_id: Culqi.token.id }) });
      Culqi.close();
    } else if (Culqi.order) {
      // solo si usaste settings.order — flujo PagoEfectivo/Yape
    } else {
      console.error("Culqi error:", Culqi.error);
    }
  };

  document.getElementById("btn-suscribir").addEventListener("click", e => {
    e.preventDefault();
    Culqi.open();
  });
</script>
```

Puntos críticos:
- El handler global se asigna a **`Culqi.culqi`** (NO un `addEventListener`). Es una función nombrada o anónima asignada a la propiedad.
- Después de tokenizar, **siempre llamar `Culqi.close()`** o queda el modal abierto.
- El token solo guarda los datos de tarjeta cifrados — **no es la tarjeta guardada**. Para reusarla en cobros recurrentes hay que: 1) crear customer, 2) crear card a partir del token. El token vence rápido (típicamente minutos), la card no.
- `Culqi.error` es un objeto JSON con `type`, `code`, `merchant_message`, `user_message` (ver §10).

Fuente: [docs.culqi.com/es/documentacion/checkout/checkout-custom](https://docs.culqi.com/es/documentacion/checkout/checkout-custom)

---

## 4. Customers — `/v2/customers`

### POST /v2/customers (crear)

Headers: `Authorization: Bearer sk_*`, `Content-Type: application/json`.

Body (**todos los campos marcados son required**):

| Campo | Tipo | Req | Notas |
|---|---|---|---|
| `first_name` | string 2-50 | ✓ | Solo alfabéticos + espacios. Regex: `^[^0-9±!@£$%^&*_+§¡€#¢§¶•ªº«\<>-?:;|=.,]{2,50}$` |
| `last_name` | string 2-50 | ✓ | Idem |
| `email` | string 5-50 | ✓ | Formato email |
| `address` | string 5-100 | ✓ | Dirección física. Culqi la requiere — no se puede mandar `null` |
| `address_city` | string 2-30 | ✓ | Ciudad |
| `country_code` | string | ✓ | ISO-3166-1 Alpha-2 (ej. `"PE"`) |
| `phone_number` | string 5-15 | ✓ | **REQUIRED según docs.** Si no se tiene del usuario hay que pedirlo o mandar un placeholder válido (5+ chars) |
| `metadata` | object | — | máx 20 pares, key ≤30 chars, value ≤200 chars |

⚠ **Bug latente en `CulqiService.cs:54-69`:** el método `CreateCustomerAsync` envía `phone_number = null` cuando el llamador no lo pasa. Culqi rechazará con `parameter_error` / HTTP 422. Hay que asegurar que siempre se envíe un string ≥5 chars (o pedírselo al usuario en el form de suscripción).

Respuesta 201:
```json
{
  "object": "customer",
  "id": "cus_test_Lz6Yfsm7QqCPIECW",
  "creation_date": 1487041774773,
  "email": "...",
  "antifraud_details": { ... },
  "metadata": {}
}
```

### Otros endpoints
- `GET /v2/customers` — listar con filtros (`first_name`, `last_name`, `email`, `country_code`, `limit`, `before`, `after`).
- `GET /v2/customers/{id}` — incluye lista de `cards` asociadas.
- `PATCH /v2/customers/{id}` — actualiza cualquier campo (todos opcionales).
- `DELETE /v2/customers/{id}` — borrar.

**Relación con cards/subscriptions:** un customer puede tener N cards. Una card pertenece a UN customer. Una subscription se crea contra una `card_id` (la cuenta de cliente se infiere de la card; no se envía `customer_id` al crear suscripción).

Fuente: [apidocs.culqi.com/#tag/Clientes](https://apidocs.culqi.com/#tag/Clientes)

---

## 5. Cards — `/v2/cards`

### POST /v2/cards (crear / guardar tarjeta)

```json
{
  "customer_id": "cus_test_xxxxxxxxxxxxxxxxx",
  "token_id":    "tkn_test_xxxxxxxxxxxxxxxxx",
  "validate":    false,
  "metadata":    {}
}
```

- `validate: true` hace un cargo de S/1.00 (100 céntimos) y lo revierte para verificar la tarjeta. Útil pero cobra al tarjetahabiente la nota del banco. **Por defecto usar `false`** para suscripciones (el primer cargo real ya validará).
- Una vez creada, la card tiene ID `crd_test_*` / `crd_live_*` y puede usarse N veces.

**Tarjeta de pago único vs guardada:**
- **Pago único:** se hace `POST /v2/charges` con `source_id = tkn_*` (el token). No requiere customer ni card. El token se "consume" en el cargo y muere.
- **Tarjeta guardada (one-click / recurrente):** se crea customer → card (a partir del token) → cargos posteriores usan `source_id = crd_*` (cargo single) o `card_id = crd_*` (suscripción).

### Otros endpoints
- `GET /v2/cards`, `GET /v2/cards/{id}`, `PATCH /v2/cards/{id}` (cambia customer asociado), `DELETE /v2/cards/{id}`.

Fuente: [apidocs.culqi.com/#tag/Tarjetas](https://apidocs.culqi.com/#tag/Tarjetas)

---

## 6. Recurrent: Plans — `/v2/recurrent/plans`

### POST (crear plan) ⚠

Path: `POST /v2/recurrent/plans` (lo que tenemos hoy) **o** `POST /v2/recurrent/plans/create` (lo que muestra el botón del Redoc). Ver §0 — probar en sandbox.

Body canónico (según [guía oficial](https://docs.culqi.com/es/documentacion/pagos-online/recurrencia/suscripciones/plan)):

```json
{
  "name": "Plan de Prueba.",
  "short_name": "plan-de-prueba-001",
  "description": "Descripción Plan de Prueba",
  "amount": 5,
  "currency": "PEN",
  "interval_unit_time": 1,
  "interval_count": 1,
  "initial_cycles": {
    "count": 0,
    "has_initial_charge": false,
    "amount": 0,
    "interval_unit_time": 1
  },
  "metadata": { "DNI": 123456782 }
}
```

| Campo | Tipo | Req | Notas |
|---|---|---|---|
| `name` | string 5-50 | ✓ | Nombre humano del plan |
| `short_name` | string 5-50 | ✓ | Slug/alias |
| `description` | string 5-200 | ✓ | Descripción libre |
| `amount` | integer 300-5000000 | ✓ | **Céntimos.** S/1.00 = 100. Mínimo 300 (= S/3.00) |
| `currency` | enum `"PEN"` \| `"USD"` | ✓ | ⚠ Es `currency`, NO `currency_code`. La guía y Redoc coinciden. El SDK .NET README está desactualizado. Nuestro `CulqiService.cs:101` actualmente envía `currency_code` — **bug a corregir** |
| `interval_unit_time` | number enum | ✓ | Unidad temporal del intervalo. Ver tabla abajo |
| `interval_count` | number | ✓ | Cuántos intervalos entre cargos. `0` = cobro indefinido |
| `initial_cycles` | object | ✓ | **Required** según docs/Redoc. Nuestro `CulqiService` actualmente NO lo envía. Para "sin ciclo inicial diferenciado" mandar `{count:0, has_initial_charge:false, amount:0, interval_unit_time:1}` |
| `metadata` | object | — | Para guardar nuestro `tukifact_plan_id` |
| `image` | string URL | — | Opcional |

**Mapping `interval_unit_time` (CRÍTICO — confirmado contra apidocs.culqi.com):**

| Valor | Significado |
|---|---|
| `1` | Diario |
| `2` | Semanal |
| **`3`** | **Mensual** ← lo que usamos en TukiFact |
| `4` | Anual |
| `5` | Trimestral |
| `6` | Semestral |

Esto está confirmado tanto en el Objeto-plan como en la sección Crear-plan del Redoc.

> ⚠ **Importante**: `CulqiService.cs:104` envía hoy un campo `duration = 0` que **no está documentado** en la API actual (probablemente sea un residuo de la API legacy). No está claro si Culqi lo ignora silenciosamente o devuelve `parameter_error`. Recomendación: quitarlo en el próximo refactor y sustituir por `initial_cycles`.

Respuesta 201:
```json
{ "id": "pln_test_XXXXXXXXXXXXXXXX", "slug": "uuid-v4" }
```

### Otros endpoints

- `GET /v2/recurrent/plans?status=1` — listar (filtros: `amount`, `status`, `min_amount`, `max_amount`, `creation_date_from/to`, `limit`, `before`, `after`).
- `GET /v2/recurrent/plans/{id}` — detalle. La respuesta de listar usa `currency_code` y `interval` (string `"Meses"`) — sí, Culqi devuelve campos distintos a los que recibe en POST. Esto es feo pero es como está.
- `PATCH /v2/recurrent/plans/{id}` — actualizar.
- `DELETE /v2/recurrent/plans/{id}` — eliminar.

`status` enum: `1 = Activo`, `2 = Inactivo`.

Fuente: [apidocs.culqi.com/#tag/Planes](https://apidocs.culqi.com/#tag/Planes), [docs.culqi.com Planes](https://docs.culqi.com/es/documentacion/pagos-online/recurrencia/suscripciones/plan)

---

## 7. Recurrent: Subscriptions — `/v2/recurrent/subscriptions`

### POST (crear suscripción) ⚠

Path: `POST /v2/recurrent/subscriptions` (actual) o `/v2/recurrent/subscriptions/create` (Redoc). Ver §0.

Body:

```json
{
  "card_id": "crd_test_xxxxxxxxxxx",
  "plan_id": "pln_test_xxxxxxxxxxx",
  "tyc": true,
  "metadata": { "tukifact_subscription_id": "..." }
}
```

| Campo | Tipo | Req | Notas |
|---|---|---|---|
| `card_id` | string | ✓ | ID de la card (NO el token) |
| `plan_id` | string | ✓ | ID del plan en Culqi (`pln_*`) |
| `tyc` | boolean | — (recomendado `true`) | Indicador de aceptación de Términos y Condiciones. Documentado como required en la guía, opcional en el Redoc — mandar siempre `true` para evitar bordes |
| `metadata` | object | — | Hasta 20 pares |

Respuesta 201:
```json
{
  "id": "sxn_test_XXXXXXXXXXXXXXXX",
  "customer_id": "cus_test_...",
  "plan_id": "pln_test_...",
  "status": 1,
  "created_at": 1669170224000,
  "metadata": { ... }
}
```

### Lifecycle states (subscription `status`)

| Valor | Estado (objeto) | Estado (lista) | Significado |
|---|---|---|---|
| `1` | Creada | Creada | Recién creada, esperando primer cobro |
| `2` | Días de prueba | Periodo de prueba | En trial |
| `3` | Activa | Activa | Cobros mensuales corriendo |
| `4` | Cancelada | Cancelada | Cancelada manualmente o por exceso de reintentos |
| `5` | En cola | En cola | Pendiente de procesamiento |
| `6` | Finalizada | Vencida | Llegó al límite de ciclos o vencida |

Nota: el objeto-suscripción y el listado documentan los enums con etiquetas levemente distintas (`Finalizada` vs `Vencida` para `6`). Tratarlas como sinónimos.

### Cancelar

`DELETE /v2/recurrent/subscriptions/{id}` → 200 OK. **Es irreversible.** Detiene los cobros futuros. Lo que ya se cobró no se devuelve (eso requiere `POST /v2/refunds` separado).

> Las suscripciones también se cancelan **automáticamente** cuando se exceden los reintentos de cargo fallido. Por eso necesitamos webhook `subscription.cancelled` para detectar bajas no iniciadas por el usuario.

### Otros endpoints
- `GET /v2/recurrent/subscriptions?plan_id=&status=` — listar.
- `GET /v2/recurrent/subscriptions/{id}` — detalle (incluye `customer`, `plan`, `periods[].charges`).
- `PATCH /v2/recurrent/subscriptions/{id}` — solo permite cambiar `card_id` (cambio de medio de pago) y `metadata`.

Fuente: [apidocs.culqi.com/#tag/Suscripciones](https://apidocs.culqi.com/#tag/Suscripciones), [docs.culqi.com Suscripciones](https://docs.culqi.com/es/documentacion/pagos-online/recurrencia/suscripciones/suscripciones)

---

## 8. Webhooks ⚠

Configuración: **CulqiPanel → Eventos → Webhooks → +Añadir**. Se pega la URL pública (la nuestra: `https://api.tukifact.com.pe/v1/billing/webhook`) y se selecciona el tipo de evento.

### Eventos disponibles (por recurso)

Los docs públicos confirman que se puede subscribir a eventos de: **tokens, cargos, devoluciones, clientes, tarjetas, planes, suscripciones, órdenes**. Sin embargo Culqi **no publica la lista exacta de event names**. Los nombres típicos que aparecen en blog/SDK y se infieren de la API son:

| Recurso | Eventos comunes (a confirmar caso por caso en panel) |
|---|---|
| `charge.creation.succeeded` | Cargo recurrente exitoso |
| `charge.creation.failed` | Cargo recurrente fallido |
| `subscription.created` | Suscripción creada |
| `subscription.cancelled` | Suscripción cancelada (manual o automática) |
| `subscription.charge.succeeded` | Cobro mensual de suscripción OK |
| `subscription.charge.failed` | Cobro mensual fallido |
| `customer.created`, `card.created`, `refund.created` | Análogos |
| `order.creation.succeeded`, `order.status.changed` | Para PagoEfectivo/Yape/Cuotéalo (no aplica a nuestro caso) |

⚠ Como Culqi no documenta los nombres, **al configurar el webhook en el panel hay que copiar los event names exactos que muestra el dropdown** y guardarlos en una constante de nuestro código. Si el panel cambia los nombres más adelante, nuestros handlers rompen silenciosamente.

### Payload del evento

Estructura genérica (de la sección Eventos del Redoc):
```json
{
  "id": "evt_test_xxxxxxxxxxx",
  "object": "event",
  "type": "charge.creation.succeeded",
  "creation_date": 1656201600000,
  "data": { /* el objeto del recurso, ej. el charge completo */ }
}
```

### ⚠ Firma HMAC — NO documentada públicamente

**Esto es un hueco real en los docs.** Buscando en apidocs.culqi.com, docs.culqi.com, los SDKs oficiales y el blog: **no hay documentación pública del header de firma ni del algoritmo HMAC** que Culqi usa (o no usa) para webhooks.

Estado actual en TukiFact (`CulqiService.cs:136-147`):
- Asume header **`culqi-signature`** (en minúsculas).
- Asume **HMAC-SHA256(secret_key, raw_body)** codificado en **hex lowercase**.
- Comparación constant-time con `CryptographicOperations.FixedTimeEquals`.

**Esto es una conjetura.** Posibilidades reales:
1. Culqi NO firma sus webhooks (basta con que la URL sea https + secret en query/path).
2. Sí firma, pero con otro header (`x-culqi-signature`, `x-culqi-webhook-signature`, `signature`, etc.) o con otro algoritmo (HMAC-SHA1, base64 en vez de hex, JSON canonicalizado en vez de raw).
3. Firma con un secret distinto al `sk_*` (un "webhook secret" separado, como hace Stripe).

**Acción requerida antes de pasar a live:** abrir ticket con Culqi soporte (`hola@culqi.com` o canal soporte del panel) preguntando textualmente: *"¿cuál es el header HTTP que envían en los webhooks para verificar autenticidad, qué algoritmo HMAC usan, qué se firma (raw body o JSON normalizado) y con qué secret?"*. Mientras tanto, confiar en HTTPS + idempotencia + filtrar por IP origen (también no documentada, hay que pedirla).

Si Culqi confirma que NO firma → quitar la verificación y agregar otro mecanismo (signed URL query token, IP allowlist, mutual TLS).

Fuente: [docs.culqi.com/es/documentacion/pagos-online/webhooks](https://docs.culqi.com/es/documentacion/pagos-online/webhooks) — la página entera no menciona firmas.

---

## 9. Tarjetas de prueba (sandbox)

Para sandbox usar correo `review@culqi.com` cuando se piden datos del titular.

### Compras exitosas

| Marca | Número | Exp | CVV | Tipo |
|---|---|---|---|---|
| Visa débito | `4111 1111 1111 1111` | 09/30 | `123` | Débito |
| Visa crédito | `4111 1111 1010 1113` | 09/30 | `123` | Crédito |
| Mastercard | `5111 1111 1111 1118` | 12/30 | `039` | Crédito |
| Amex | `3712 1212 1212 122` | 12/30 | `2841` | Crédito |
| Diners | `360012 1212 1210` | 12/30 | `964` | Crédito |

### Autenticación 3DS (Visa Classic, Mastercard Classic)

| Número | Resultado |
|---|---|
| `4456 5300 0000 1096` (07/30, CVV 111) | 3DS OK con challenge |
| `4456 5300 0000 1005` | 3DS OK sin challenge (frictionless) |
| `4456 5300 0000 1070` | 3DS falla por timeout |
| `4456 5300 0000 1013` | 3DS falla por error de autenticación |
| `5200 0000 0000 1096` | Mastercard 3DS OK con challenge |
| `5200 0000 0000 1005` | Mastercard 3DS frictionless |
| `5200 0000 0000 1070` | Mastercard 3DS timeout |
| `5200 0000 0000 1013` | Mastercard 3DS error |

### Casos de error (denegaciones específicas)

| Número | Resultado |
|---|---|
| `4000 0200 0000 0000` (10/30, CVV 354) | `stolen_card` |
| `4000 0300 0000 0009` (08/30, CVV 360) | `lost_card` |
| `4000 0400 0000 0008` (03/30, CVV 295) | `insufficient_funds` |
| `5400 0000 0000 0005` (01/30, CVV 492) | `contact_issuer` |
| `5400 0200 0000 0003` (07/30, CVV 203) | `incorrect_cvv` |
| `3700 010000 00000` (04/30, CVV 2511) | `issuer_not_available` |
| `3700 020000 00008` (05/30, CVV 1810) | `issuer_decline_operation` |
| `3600 000000 0008` (09/30, CVV 683) | `invalid_card` |
| `3600 010000 0007` (12/30, CVV 820) | `processing_error` |
| `3600 020000 0006` (01/30, CVV 230) | `fraudulent` |

### Casos de error con códigos Culqi específicos (Visa crédito, exp 09-12/30, CVV 123)

| Número | Código |
|---|---|
| `4111 1100 0000 0013` (12/30) | `DNGE0087` |
| `4111 1100 0000 0021` | `CULQ0001` |
| `4111 1100 0000 0039` | `CULQ0003` |
| `4111 1100 0000 0047` | `PREV0009` |
| `4111 1100 0000 0054` | `PREV0091` |
| `4111 1100 0000 0062` | `PREV0001` |
| `4111 1100 0000 0070` | `DNGA0323` |

### Yape sandbox

- Nro celular: `900 000 001`
- Código de aprobación: cualquier 6 dígitos

Fuente: [docs.culqi.com/es/documentacion/pagos-online/tarjetas-de-prueba](https://docs.culqi.com/es/documentacion/pagos-online/tarjetas-de-prueba/)

---

## 10. Errores comunes

### Objeto de error (formato de respuesta)

```json
{
  "object": "error",
  "type": "card_error",
  "code": "DNGE0015",
  "charge_id": "chr_test_xxx",
  "decline_code": "insufficient_funds",
  "merchant_message": "La tarjeta no tiene fondos suficientes...",
  "user_message": "Su tarjeta no tiene fondos suficientes...",
  "param": "amount"
}
```

- `merchant_message` → para tu log/dashboard.
- `user_message` → para mostrar al usuario.

### Tipos de error y mapping a HTTP

| `type` | HTTP | Cuándo |
|---|---|---|
| `invalid_request_error` | 400 | JSON malformado / sintaxis mala |
| `authentication_error` | 401 | Llave inválida o ausente |
| `parameter_error` | 422 | Algún campo inválido (ver `param`) |
| `card_error` | 402 | Tarjeta rechazada por banco (ver `decline_code`) |
| `limit_api_error` | 429 | Rate limit |
| `resource_error` | 404 | Recurso no existe o estado no permitido |
| `api_error` | 500/503 | Error interno Culqi |

### `decline_code` de bancos (cuando `type=card_error`)

`expired_card`, `stolen_card`, `lost_card`, `insufficient_funds`, `contact_issuer`, `invalid_cvv`, `too_many_attempts_cvv`, `issuer_not_available`, `issuer_decline_operation`, `invalid_card`, `processing_error`, `fraudulent`, `culqi_card` (estás usando una tarjeta de test en producción), `soft_block` (reintentos excedidos).

### Códigos específicos de tokenización / API que vale conocer

- `object_invalid` — payload con formato OK pero objeto no válido.
- `invalid_token` — `tkn_*` ya consumido, vencido, o de otro merchant.
- `invalid_card` — `crd_*` no existe o pertenece a otro merchant.
- `invalid_amount` — `amount < 300` (mínimo S/3.00).
- `DNGA0019` — código de comercio (llave) no válido.

Fuente: [apidocs.culqi.com/#section/Errores](https://apidocs.culqi.com/#section/Errores) y [lista completa PDF](https://apidocs.culqi.com/pdf/lista_errores.pdf)

---

## 11. Test panel vs live panel — ¿qué se hereda?

**Respuesta corta: NADA se hereda. Son cuentas separadas.**

| Recurso | ¿Se copia de test a live al activar producción? |
|---|---|
| API keys (`pk_*`, `sk_*`) | No — se generan nuevas al aprobar el merchant |
| RSA keys | No — hay que regenerar en panel live |
| **Customers** | No — base de datos separada |
| **Cards** (`crd_*`) | No — los IDs `crd_test_*` no existen en live |
| **Plans** (`pln_*`) | **No — hay que recrearlos en live antes de la primera suscripción real** |
| **Subscriptions** (`sxn_*`) | No — empieza de cero |
| **Charges, refunds, orders, events** | No — historial separado |
| **Webhooks** (URLs configuradas) | No — hay que reconfigurar en panel live |
| **Datos del comercio** (razón social, cuenta bancaria) | Sí — vienen del onboarding aprobado |

**Implicación para TukiFact (hoy con `pk_test_dYUtcOLqXDWhHdoB` + `sk_test_z8JB359lX6XNVCEw`):**

1. Cuando Culqi apruebe el comercio, generar nuevas `pk_live_*` + `sk_live_*` en `https://panel.culqi.com/`.
2. Setear esas vars en producción (`Culqi__SecretKey`, `NEXT_PUBLIC_CULQI_PUBLIC_KEY`).
3. **Crear los 5 planes (Emprendedor, Negocio, Profesional, Empresa — el Gratis no necesita plan en Culqi) en producción usando nuestro `EnsurePlanAsync` o manualmente desde el panel.** Confirmar que `plan.CulqiPlanId` en la tabla `Plans` queda actualizado con los nuevos `pln_live_*`.
4. Reconfigurar el webhook `https://api.tukifact.com.pe/v1/billing/webhook` apuntando a `https://panel.culqi.com/` (no al integ-panel).
5. Las suscripciones de prueba se quedan en test — los clientes reales empiezan a suscribirse desde el primer momento contra el panel live.

⚠ Si ya hay clientes reales suscriptos en sandbox por accidente, **no hay forma de migrarlos**. Tienen que volver a meter su tarjeta en el flujo live.

Fuente: por inferencia de la separación de los hosts (`integ-panel.culqi.com` vs `panel.culqi.com`) — Culqi no publica un documento explícito de "migración". Confirmado al hablar con onboarding típicamente.

URLs de paneles:
- Integración / test: https://integ-panel.culqi.com/
- Producción / live: https://panel.culqi.com/ (alias histórico: http://mipanel.culqi.com/)

Fuente: [apidocs.culqi.com/#section/Autenticacion](https://apidocs.culqi.com/#section/Autenticacion)

---

## 12. Onboarding y observaciones (checklist típico de Culqi)

Cuando subís el sitio a revisión, Culqi típicamente observa estos items. Si falta alguno, el merchant queda en "Subsanación" hasta arreglarlo.

### Producto/servicio (lo más observado)
- [ ] **Al menos 5 productos/servicios o planes visibles** en la web pública (no detrás de login). Para TukiFact: la página `/plan` ya cumple con los 5 planes (Gratis + 4 pagos) y al estar accesible sin login deberíamos pasar este punto.
- [ ] Cada uno con **imagen/icono + descripción + precio claro**. Los íconos por tier que tenemos hoy ayudan.
- [ ] **Carrito o botón de pago activo** (el "Suscribirme" tiene que abrir el checkout — no decir "próximamente").
- [ ] Si el producto requiere registro/login para verse, **proveer credenciales demo en el form de revisión** (ej. `review@culqi.com` / `Culqi2025!`).

### Documentos legales obligatorios (links en footer)
- [ ] **Términos y Condiciones** — ya tenemos.
- [ ] **Política de Privacidad** — ya tenemos.
- [ ] **Política de Devoluciones** — ya tenemos (`/devoluciones`).
- [ ] **Libro de Reclamaciones** — ya tenemos (`/reclamaciones` + API).

### Datos del comercio visibles
- [ ] **Razón social + RUC** en footer/Contacto/T&C.
- [ ] **Dirección fiscal** (la nuestra: Av. Javier Prado o donde corresponda).
- [ ] **Teléfono de contacto real** (no genérico) — ya cambiado.
- [ ] **Email de soporte** (`soporte@tukifact.com.pe` o similar) que efectivamente responda.

### Técnico
- [ ] HTTPS válido en todo el dominio (cert no expirado, sin contenido mixto).
- [ ] Webhook URL configurada en panel test para validar el flujo end-to-end antes de live.
- [ ] Flujo de cobro probado con las tarjetas de §9 (al menos: éxito, insufficient_funds, 3DS challenge).

Fuente: inferido del proceso típico Culqi (no hay un único documento checklist publicado). Las observaciones reales llegan por email del onboarding officer.

---

## 13. Configuración del panel (URLs útiles)

Panel test: **https://integ-panel.culqi.com/**
Panel live: **https://panel.culqi.com/**

Rutas dentro del panel (ambas instancias tienen la misma estructura):

| Acción | Ruta |
|---|---|
| Ver / regenerar API keys | `Desarrollo → API Keys` |
| Generar llaves RSA (encriptación payload, opcional) | `Desarrollo → RSA Keys` |
| Configurar webhook URL | `Eventos → Webhooks → +Añadir` |
| Ver logs de webhooks enviados (reintentos, status) | `Eventos → Webhooks → <webhook> → Historial` |
| Listar suscripciones activas | `Recurrencia → Suscripciones` |
| Listar clientes y sus tarjetas guardadas | `Clientes` |
| Crear/editar planes manualmente | `Recurrencia → Planes → +Nuevo plan` |
| Ver cargos (todos) | `Pagos → Cargos` |
| Crear devolución manual | `Pagos → Cargos → <cargo> → Devolver` |
| Estado de cuenta y depósitos | `Abonos → Estado de cuenta` |
| Calendario de depósitos (cuándo llega el dinero) | `Abonos → Calendario de depósitos` |

Notas:
- Las "URLs profundas" dentro del panel son SPA hashes (`#/desarrollo/api-keys`, `#/eventos/webhooks`) — no funcionan compartidas, hay que loguearse primero.
- El historial de webhooks muestra cada intento con código HTTP de la respuesta, body recibido y body enviado. Es el lugar #1 para debuggear webhooks que no llegan o que el endpoint rechaza.

Fuente: navegación directa del panel (la estructura no está documentada formalmente en docs.culqi.com).

---

## Apéndice — Cheatsheet de IDs

| Prefijo | Recurso |
|---|---|
| `tkn_test_*` / `tkn_live_*` | Token de tarjeta (efímero) |
| `cus_test_*` / `cus_live_*` | Customer |
| `crd_test_*` / `crd_live_*` | Card guardada |
| `chr_test_*` / `chr_live_*` | Charge |
| `pln_test_*` / `pln_live_*` | Plan recurrente |
| `sxn_test_*` / `sxn_live_*` | Subscription |
| `ord_test_*` / `ord_live_*` | Order (PagoEfectivo/Yape/Cuotéalo) |
| `evt_test_*` / `evt_live_*` | Event (webhook payload) |
| `rfd_test_*` / `rfd_live_*` | Refund |

## Apéndice — Referencias

- API reference (Redoc): https://apidocs.culqi.com/
- Guía oficial: https://docs.culqi.com/
- SDKs oficiales: https://github.com/culqi (ojo: el README .NET tiene API legacy)
- Lista completa de códigos de error: https://apidocs.culqi.com/pdf/lista_errores.pdf
- Tarjetas de prueba: https://docs.culqi.com/es/documentacion/pagos-online/tarjetas-de-prueba/
- Webhooks: https://docs.culqi.com/es/documentacion/pagos-online/webhooks
- Checkout Custom: https://docs.culqi.com/es/documentacion/checkout/checkout-custom
- Soporte: https://culqi.com/canales-de-atencion
