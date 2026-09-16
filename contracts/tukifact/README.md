# Contrato TukiFact ↔ proyectos partner

**Estado: contrato v1 cerrado en su forma; implementación en curso en TukiFact.** El flujo completo, las decisiones y los cambios por repositorio están en [`docs/integracion/flujo-emision-partners.md`](../../docs/integracion/flujo-emision-partners.md) (repo TukiFact). Este README resume lo que un consumidor necesita para generar su cliente y consumir eventos.

**Copia autoritativa.** Mientras no exista un repositorio de contratos, la copia autoritativa vive en `TukiFact/contracts/tukifact/v1/cpe.proto`. Market Real y ToroLoco mantienen una copia byte-idéntica en la misma ruta relativa (`contracts/tukifact/`). Cualquier cambio entra primero en TukiFact y después se sincroniza; nadie edita la copia local.

## Quién habla con quién

```text
Caja / POS → backend del partner → TukiFact → SUNAT
                     ↑                  │
                     └── resultado (CDR) ┘
```

El POS **nunca** llama a TukiFact: siempre pasa por el backend de su proyecto, que es el dueño de la venta, de los permisos y de su propio outbox. TukiFact es la única autoridad de numeración (asigna serie y correlativo) y el dueño del certificado, de la firma, del envío a SUNAT y de la custodia de XML, CDR y PDF.

| Consumidor | Stack | Cliente generado va en |
|---|---|---|
| Market Real | .NET 10 | Infrastructure del módulo que emita (adapter gRPC) |
| ToroLoco | NestJS 11 | Provider gRPC que reemplaza el cliente REST de `TukiFactPort` |

## Modelo de cuentas

| Concepto | Definición |
|---|---|
| Partner | El proyecto integrador (Market Real, ToroLoco). Tiene una URL de webhook y un secreto propios (uno por partner, no por tenant) y una API key de partner para aprovisionar tenants. |
| Tenant | Una empresa emisora, identificada por su RUC. Pertenece a un partner o es cliente directo de TukiFact. |
| API key de tenant | Credencial de máquina emitida al aprovisionar el tenant. Autentica todas las llamadas gRPC de ese RUC (`authorization: ApiKey <tk_...>`). La key resuelve el tenant; no se envía `x-tenant-id`. |
| `emission_point` | Identificador que el partner asigna a cada caja o sede. TukiFact mantiene una serie por punto de emisión y tipo de documento. |
| Plan `partner-unlimited` | Plan de los tenants aprovisionados por partner: documentos ilimitados, facturación fuera de Culqi, alta solo por aprovisionamiento. Solo facturación (01, 03, 07, 08, RA); el resto de módulos responde `Tenant.PlanFeatureDisabled`. |

Reglas para proyectos por sede (ToroLoco): una sede con RUC propio es un tenant propio con su API key; varias sedes con el mismo RUC comparten tenant y API key y se distinguen por `emission_point`. El contador local de correlativos del partner deja de ser fuente fiscal: la identidad del comprobante en el partner es `client_document_id`.

## Las tres piezas del contrato

| Pieza | Mecanismo | Obligatoria | Para qué |
|---|---|---|---|
| `SignAndEnqueue` | gRPC | Sí | Firmar, numerar y encolar el envío a SUNAT. Devuelve `hash_code` y `qr_data` en milisegundos para imprimir el ticket. |
| `DocumentEvent` | Webhook HTTPS firmado, una URL y un secreto por partner | Sí | Recibir el resultado de SUNAT sin hacer polling. Entrega durable con reintentos hasta 72 h. |
| `ListResults(since_cursor)` | gRPC | Sí | Reconciliar por cursor al arrancar y periódicamente; garantiza que un CDR no se pierda si el consumidor estuvo caído o si una entrega agotó sus reintentos. |

Las tres son obligatorias. El pull no es opcional.

RPCs completos de `tukifact.v1.CpeService`: `SignAndEnqueue`, `VoidDocument`, `GetDocument`, `ListDocuments`, `ListResults`.

### Estados del documento

| Estado | Significado | Evento |
|---|---|---|
| `signed` | Firmado y numerado; pendiente de envío | `document.signed` |
| `accepted` | SUNAT aceptó | `document.accepted` |
| `observed` | SUNAT aceptó con observaciones | `document.observed` |
| `rejected` | SUNAT rechazó; el correlativo queda consumido | `document.rejected` |
| `voided` | Baja aceptada por SUNAT | `document.voided` |

`rejected` es terminal: corregir exige un documento nuevo. Los estados internos de TukiFact no forman parte del contrato.

### Notas de crédito y débito

Mismo `SignAndEnqueue` con `document_type` `07` u `08`, más `reference` (por `document_id` o por clave de negocio `document_type` + `full_number`) y `note_reason` (catálogo 09 para 07, catálogo 10 para 08). Ambos campos son obligatorios si y solo si el tipo es 07/08.

## Convenciones que no se negocian

- **Códigos de catálogo SUNAT como `string`** con su valor tal cual (`"01"`, `"10"`, `"6"`). Sin capa de mapeo que se desincronice.
- **Dinero y cantidades como `string` decimal** de punto fijo. `double` no sirve para importes y proto no tiene decimal.
- **Fechas de negocio** `"YYYY-MM-DD"`; instantes técnicos en `Timestamp` UTC.
- **Metadata obligatoria**: `authorization: ApiKey <tk_...>`, `x-correlation-id`, y en mutaciones `idempotency-key` igual a `client_document_id`. `traceparent` (W3C) recomendado. TLS obligatorio; rate limit por key.
- **Idempotencia**: la repetición con la misma key devuelve el mismo documento con `idempotent_replay = true`; la misma key con cuerpo distinto responde `ABORTED` / `Idempotency.PayloadMismatch`. Si algo falla, no se consume correlativo.
- **Serie y correlativo los asigna TukiFact.** El partner manda `emission_point`, nunca el número.
- **Errores** con el contrato único del proyecto: `google.rpc.Status` con `ErrorDetail` en `details`, `code` estable (`<Ámbito>.<Motivo>`), `type` como categoría. El cliente ramifica por `code`, nunca por el texto. Ejemplos: `ApiKey.Invalid`, `Tenant.PlanFeatureDisabled`, `Idempotency.PayloadMismatch`, `Series.Exhausted`, `Tenant.CertificateMissing`, `SignAndEnqueue.Validation`.

## Entrega por webhook

- **Endpoint**: una URL y un secreto por partner (no por tenant), registrados al aprovisionar el partner (backoffice o API de aprovisionamiento). El secreto se muestra una sola vez y se rota con ventana de solapamiento (dos secretos válidos durante la rotación). Los eventos llevan el tenant en header y en payload, así que ToroLoco con muchas sedes recibe todo en una sola URL.
- **Request**: `POST <url>` con `Content-Type: application/json`. Body: mensaje `DocumentEvent` en JSON canónico de protobuf (camelCase), sin envelope de ninguna librería. Incluye `cursor` para continuar con `ListResults`.
- **Ack**: cualquier `2xx` dentro de 10 segundos. El receptor encola y responde rápido; nunca procesa lógica SUNAT en línea.
- **Reintentos**: desde la tabla durable de entregas del outbox, con backoff exponencial 1 min, 5 min, 30 min, 2 h, 6 h y luego cada 12 h hasta 72 h. Agotados, la entrega queda `failed` (visible en el backoffice de TukiFact) y el evento sigue recuperable con `ListResults`.
- **Garantía**: al menos una vez; el orden **no** está garantizado bajo reintentos. La garantía de dedupe es el inbox del consumidor con `event_id` único y constraint de dominio.

Headers obligatorios:

| Header | Valor |
|---|---|
| `X-Event-Id` | `event_id` (UUID), clave de dedupe en el inbox del receptor |
| `X-Event-Type` | Igual al campo `event_type` |
| `X-Schema-Version` | `1` |
| `X-Tenant-Id` | Tenant emisor (también viaja en el payload) |
| `X-Correlation-Id` | El mismo que viajó en `SignAndEnqueue` |
| `X-Delivery-Attempt` | Número de intento, `1..n` |
| `traceparent` | Contexto W3C para la traza distribuida |
| `X-TukiFact-Signature` | `t=<unix seconds>,v1=<hex HMAC-SHA256(secret, "<t>.<raw body>")>` |

Verificación de firma: el receptor rechaza si `|now − t| > 5 minutos` o si la firma no coincide (protección contra replay). Durante una rotación acepta cualquiera de los dos secretos vigentes. Este formato con marca de tiempo reemplaza al `sha256=<hmac(body)>` del `WebhookDeliveryService` actual de TukiFact; v1 del contrato partner usa solo la forma con `t=`.

Obligaciones del receptor:

| Obligación | Detalle |
|---|---|
| Verificar firma | Antes de leer el body; rechazar con `401` si falla |
| Inbox | Insertar `X-Event-Id` con constraint único; duplicado → `200` y descartar |
| Máquina de estados | Los estados terminales ganan: `accepted` / `observed` / `rejected` / `voided` sobre `signed` |
| Cursor | Persistir el `cursor` del último evento procesado |
| Reconciliación | `ListResults` al arrancar y periódicamente (recomendado cada 5 minutos) desde el último cursor |

## Seguridad

- **gRPC**: TLS obligatorio; autenticación por API key de tenant en metadata `authorization`. Rate limit por key.
- **Webhook**: solo HTTPS con TLS 1.2+. Firma HMAC-SHA256 con marca de tiempo en `X-TukiFact-Signature`. Allowlist opcional de las IPs de egreso de TukiFact en el partner. El secreto nunca vive en el repo.
- **NATS**: interno a TukiFact (outbox → NATS → handlers propios). No se expone a internet y no forma parte del contrato público.

## Versionado

- Versión en el paquete, en el servicio y en el header de cada evento: `tukifact.v1`, `tukifact.v1.CpeService`, `X-Schema-Version: 1`.
- Dentro de `v1` se agregan campos; no se renumeran, borran ni cambian de significado.
- Un cambio incompatible nace como `tukifact.v2` y convive con `v1` hasta que el último consumidor migre.
- Los eventos llevan `X-Schema-Version` en headers; el consumidor rechaza versiones que no conoce.

## Generar clientes

```bash
# .NET (Market Real): paquetes en Directory.Packages.props / el proyecto que consume
dotnet add package Grpc.Net.Client
dotnet add package Grpc.Tools
dotnet add package Google.Protobuf
# en el .csproj:
#   <Protobuf Include="..\..\contracts\tukifact\v1\cpe.proto" GrpcServices="Client" />
# eventos: endpoint HTTPS receptor (Minimal API) + tabla inbox por event_id + job ListResults

# NestJS (ToroLoco)
pnpm add @nestjs/microservices @grpc/grpc-js @grpc/proto-loader
# ClientsModule.register({ transport: Transport.GRPC, options: { package: 'tukifact.v1', protoPath } })
# eventos: controller HTTPS receptor + tabla inbox por event_id + job ListResults
```

El receptor de eventos no necesita ninguna librería de mensajería: es un endpoint HTTPS que verifica `X-TukiFact-Signature` con el secreto del partner, inserta `X-Event-Id` en su inbox y responde `2xx`. El `DocumentEvent` se deserializa con el JSON canónico del cliente generado o, en NestJS, directamente como objeto si el proyecto prefiere no generar código. En Market Real, Wolverine y RabbitMQ quedan internos; nunca tocan el contrato.

## Decisiones y pendiente

Los pendientes del borrador quedaron resueltos así:

| # | Decisión |
|---|---|
| PENDING-1 | API key de tenant en metadata `authorization: ApiKey <key>`; la key resuelve el tenant, no se envía `x-tenant-id`. |
| PENDING-2 | Firmar ahora, enviar después: `SignAndEnqueue` devuelve `SignedDocument`; el resultado de SUNAT llega por evento. |
| PENDING-3 | Eventos `document.signed` / `accepted` / `observed` / `rejected` / `voided`. |
| PENDING-4 | Entrega de resultados por webhook firmado por partner (una URL y un secreto por partner, HMAC-SHA256 con marca de tiempo, reintentos durables hasta 72 h). NATS JetStream queda interno a TukiFact y no se expone a internet. |
| PENDING-5 | `ListResults(since_cursor)` con cursor monotónico por tenant; vía de recuperación cuando una entrega agotó sus reintentos. |
| PENDING-6 | Header `X-Schema-Version` = `1`. |
| PENDING-8 | `Reference` acepta `document_id` o `document_type` + `full_number`. |

Único pendiente abierto, y no se resuelve escribiendo código:

| # | Pendiente | Dueño |
|---|---|---|
| PENDING-7 | Tasa de ICBPER vigente y forma del resumen diario (RC) | Fiscal, TukiFact |
