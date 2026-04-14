# Mega-Sprint 4: Developer Platform — API Publica

> **Tema**: OpenAPI + Docs Interactiva + SDK Node/Python + Sandbox + Portal Developer
> **Estimacion**: ~3-4 dias
> **Prioridad**: ALTA — ES TU NEGOCIO. Vender API + SDK a otros devs.

---

## Vision

TukiFact no es solo un portal web de facturacion.
Es una **plataforma para developers** que quieren integrar facturacion electronica
peruana en SUS apps. Vos vendes la API, ellos construyen encima.

```
Tu cliente NO es solo la empresa que factura.
Tu cliente es el DEVELOPER que construye apps para empresas que facturan.
```

Esto es lo que hace Stripe con pagos. Vos lo haces con facturacion SUNAT.

---

## Contexto

Ya existe:
- `ApiKeysController.cs` — generar/revocar API keys (tk_* + SHA256)
- 40+ endpoints REST organizados en /v1/
- JWT auth + API Key auth
- InMemoryRateLimiter (falta middleware, viene de M1.5)

Lo que FALTA: documentacion, SDKs, sandbox, portal.

---

## Tareas

### M4.1 — OpenAPI 3.1 Spec Auto-Generada (MEDIO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Api/Program.cs` — configurar Swashbuckle/NSwag
- `src/TukiFact.Api/TukiFact.Api.csproj` — agregar paquetes
- Todos los controllers — agregar XML docs + `[ProducesResponseType]` + `[SwaggerOperation]`

**Setup**:
```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TukiFact API",
        Version = "v1",
        Description = "API de Facturacion Electronica para Peru. " +
            "Emite facturas, boletas, notas de credito/debito, guias de remision, " +
            "retenciones, percepciones y mas. Compatible con SUNAT.",
        Contact = new OpenApiContact
        {
            Name = "TukiFact Developer Support",
            Email = "developers@tukifact.net.pe",
            Url = new Uri("https://tukifact.net.pe/developers")
        },
        License = new OpenApiLicense { Name = "Proprietary" }
    });

    // Auth schemes
    c.AddSecurityDefinition("Bearer", ...);     // JWT
    c.AddSecurityDefinition("ApiKey", ...);     // X-API-Key header

    // XML comments
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "TukiFact.Api.xml"));

    // Tags organizados
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
});
```

**Tags (agrupacion)**:
```
Auth            — Login, Register, Refresh Token, Password Reset
Documents       — Emitir, Listar, Detalle, XML, PDF, Anular
Credit Notes    — Notas de Credito
Debit Notes     — Notas de Debito
Voided          — Comunicacion de Baja
Daily Summary   — Resumen Diario de Boletas
Despatch        — Guias de Remision (GRE)
Retentions      — Retenciones Electronicas
Perceptions     — Percepciones Electronicas
Quotations      — Cotizaciones (convertir a factura)
Recurring       — Facturacion Recurrente
Customers       — Directorio de Clientes
Products        — Catalogo de Productos/Servicios
Series          — Gestion de Series
Exchange Rates  — Tipo de Cambio SBS
Catalogs        — Catalogos SUNAT
Webhooks        — Configuracion de Webhooks
API Keys        — Gestion de API Keys
Dashboard       — Metricas y KPIs
Audit Log       — Registro de Actividad
AI Assistant    — Chat IA para facturacion
Lookup          — Consulta RUC/DNI
```

**Ejemplo de controller decorado**:
```csharp
/// <summary>
/// Emitir un comprobante electronico (factura, boleta, NC, ND)
/// </summary>
/// <remarks>
/// Crea y envia un comprobante a SUNAT. El XML se genera automaticamente
/// siguiendo UBL 2.1 y se firma con el certificado digital del tenant.
///
/// **Tipos soportados**:
/// - `01` Factura
/// - `03` Boleta
/// - `07` Nota de Credito
/// - `08` Nota de Debito
///
/// **Ejemplo request**:
/// ```json
/// {
///   "documentType": "01",
///   "serieId": "uuid-de-serie-F001",
///   "customerId": "uuid-del-cliente",
///   "items": [
///     {
///       "description": "Servicio de consultoria",
///       "quantity": 1,
///       "unitPrice": 1000.00,
///       "igvType": "gravado"
///     }
///   ]
/// }
/// ```
/// </remarks>
[HttpPost("emit")]
[SwaggerOperation(OperationId = "EmitDocument", Tags = new[] { "Documents" })]
[ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public async Task<IActionResult> Emit([FromBody] EmitDocumentRequest request, CancellationToken ct)
```

**Output**: `/swagger/v1/swagger.json` — OpenAPI 3.1 spec completa

### M4.2 — Docs Interactiva con Scalar (BAJO)
**Dependencias**: M4.1
**Archivos a crear/modificar**:
- `src/TukiFact.Api/Program.cs` — reemplazar Swagger UI con Scalar
- `src/TukiFact.Api/TukiFact.Api.csproj` — paquete Scalar.AspNetCore

**Por que Scalar y no Swagger UI**:
- Mas moderno y bonito (dark mode nativo)
- Mejor experiencia de lectura
- Code samples automaticos en 10+ lenguajes
- Mejor busqueda
- Gratis y open source

**Setup**:
```csharp
// Program.cs
app.MapScalarApiReference(options =>
{
    options.Title = "TukiFact API Documentation";
    options.Theme = ScalarTheme.Purple;  // o custom con colores TukiFact
    options.DefaultHttpClient = new(ScalarTarget.Node, ScalarClient.Fetch);
    options.Authentication = new ScalarAuthenticationOptions
    {
        PreferredSecurityScheme = "ApiKey"
    };
});
```

**Rutas**:
```
/docs          — Scalar API Reference (interactiva)
/swagger.json  — OpenAPI spec raw (para SDKs)
```

### M4.3 — API Versioning (MEDIO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Api/Program.cs` — configurar versioning
- `src/TukiFact.Api/TukiFact.Api.csproj` — paquete Asp.Versioning.Http

**Estrategia**: URL path versioning (ya usas `/v1/`)

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;  // Header: api-supported-versions
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version")
    );
});
```

**Headers de respuesta**:
```
Api-Supported-Versions: 1.0
Api-Deprecated-Versions: (none yet)
```

**Cuando agregar v2**: cuando hagas breaking changes.
Por ahora v1 es la unica version.

### M4.4 — Rate Limiting por Plan (MEDIO)
**Dependencias**: M1.5 (Mega-Sprint 1)
**Archivos a modificar**:
- `src/TukiFact.Api/Middleware/RateLimitingMiddleware.cs` — leer plan del tenant

**Ya definido en M1.5, aca se completa**:
```
Free:          100 requests/hora,    50 docs/mes
Emprendedor:   500 requests/hora,   300 docs/mes
Negocio:     2,000 requests/hora, 1,000 docs/mes
Developer:   2,000 requests/hora, 1,000 docs/mes  (API-first)
Profesional: 5,000 requests/hora, 3,000 docs/mes
Empresa:    20,000 requests/hora, 10,000 docs/mes
```

**Diferencia vs M1.5**: M1.5 pone el middleware basico.
M4.4 lo conecta con el plan real del tenant y agrega limite de documentos/mes.

**Enforcement**:
```
Si excede requests/hora → 429 + Retry-After header
Si excede docs/mes → 403 + mensaje "Limite de documentos alcanzado. Upgrade tu plan."
```

### M4.5 — SDK TypeScript/Node.js (ALTO)
**Dependencias**: M4.1
**Archivos a crear**:
- `sdk/typescript/` — directorio nuevo
- Auto-generado desde OpenAPI spec con `openapi-typescript-codegen` o `@hey-api/openapi-ts`

**Estructura SDK**:
```
sdk/typescript/
  ├── package.json          (@tukifact/sdk)
  ├── tsconfig.json
  ├── src/
  │   ├── index.ts          — export principal
  │   ├── client.ts         — TukiFact class
  │   ├── types.ts          — interfaces auto-generadas
  │   ├── resources/
  │   │   ├── documents.ts  — client.documents.emit(), .list(), .get()
  │   │   ├── customers.ts  — client.customers.create(), .list()
  │   │   ├── products.ts   — client.products.create(), .list()
  │   │   ├── series.ts     — client.series.create(), .list()
  │   │   ├── webhooks.ts   — client.webhooks.create(), .list()
  │   │   ├── quotations.ts
  │   │   ├── retentions.ts
  │   │   ├── perceptions.ts
  │   │   ├── despatch.ts
  │   │   └── recurring.ts
  │   └── errors.ts         — TukiFactError, RateLimitError, etc.
  ├── README.md
  └── examples/
      ├── emit-factura.ts
      ├── emit-boleta.ts
      ├── credit-note.ts
      ├── list-documents.ts
      └── webhooks.ts
```

**Uso del SDK**:
```typescript
import { TukiFact } from '@tukifact/sdk';

const tuki = new TukiFact({
  apiKey: 'tk_live_...',
  baseUrl: 'https://api.tukifact.net.pe',  // opcional, default prod
});

// Emitir factura
const factura = await tuki.documents.emit({
  documentType: '01',
  serieId: 'uuid-serie',
  customerId: 'uuid-cliente',
  items: [
    {
      description: 'Servicio de consultoria',
      quantity: 1,
      unitPrice: 1000.00,
      igvType: 'gravado',
    },
  ],
});

console.log(factura.serie, factura.correlativo); // F001-123
console.log(factura.sunatStatus); // 'accepted'

// Descargar PDF
const pdf = await tuki.documents.downloadPdf(factura.id);

// Listar documentos
const docs = await tuki.documents.list({
  type: '01',
  status: 'accepted',
  page: 1,
  pageSize: 20,
});

// Webhooks
const hook = await tuki.webhooks.create({
  url: 'https://miapp.com/webhook/tukifact',
  events: ['document.sent', 'document.failed'],
});

// Lookup RUC
const empresa = await tuki.lookup.ruc('20613614509');
console.log(empresa.name); // TUKITUKI SOLUTION SAC
```

**Error handling**:
```typescript
try {
  await tuki.documents.emit({ ... });
} catch (error) {
  if (error instanceof TukiFactError) {
    console.log(error.status);  // 400
    console.log(error.code);    // 'INVALID_DOCUMENT_TYPE'
    console.log(error.message); // 'Tipo de documento invalido'
  }
  if (error instanceof RateLimitError) {
    console.log(error.retryAfter); // 120 (seconds)
  }
}
```

**Generacion**:
```bash
# Auto-generar desde OpenAPI
npx @hey-api/openapi-ts \
  --input http://localhost:5000/swagger/v1/swagger.json \
  --output ./sdk/typescript/src/generated \
  --client fetch

# Luego wrappear con la clase TukiFact friendly
```

### M4.6 — SDK Python (MEDIO)
**Dependencias**: M4.1
**Archivos a crear**:
- `sdk/python/` — directorio nuevo
- Auto-generado + wrapper manual

**Estructura**:
```
sdk/python/
  ├── pyproject.toml        (tukifact)
  ├── tukifact/
  │   ├── __init__.py
  │   ├── client.py         — TukiFact class
  │   ├── types.py          — dataclasses/Pydantic models
  │   ├── resources/
  │   │   ├── documents.py
  │   │   ├── customers.py
  │   │   ├── products.py
  │   │   └── ...
  │   └── errors.py
  ├── README.md
  └── examples/
      ├── emit_factura.py
      └── list_documents.py
```

**Uso**:
```python
from tukifact import TukiFact

tuki = TukiFact(api_key="tk_live_...")

# Emitir factura
factura = tuki.documents.emit(
    document_type="01",
    serie_id="uuid-serie",
    customer_id="uuid-cliente",
    items=[{
        "description": "Servicio de consultoria",
        "quantity": 1,
        "unit_price": 1000.00,
        "igv_type": "gravado",
    }]
)

print(f"{factura.serie}-{factura.correlativo}")  # F001-123

# Listar
docs = tuki.documents.list(type="01", status="accepted")
for doc in docs.data:
    print(doc.total)
```

### M4.7 — Sandbox/Playground (MEDIO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Api/Controllers/SandboxController.cs`
- `src/TukiFact.Infrastructure/Services/SandboxService.cs`

**Concepto**: tenant de prueba auto-creado para developers.

**Endpoint**:
```
POST /v1/sandbox/create
  Body: { email, company_name }
  Response: {
    tenant_id,
    api_key: "tk_sandbox_...",
    credentials: { email, password },
    expires_at: "+30 dias",
    limits: { documents: 100, requests_per_hour: 200 }
  }
```

**Reglas sandbox**:
- Prefijo `tk_sandbox_` en API keys (vs `tk_live_` produccion)
- SUNAT siempre en modo beta (nunca envia a SUNAT real)
- Documentos generados tienen marca de agua "SANDBOX"
- PDFs tienen watermark
- Auto-cleanup: tenants sandbox se borran a los 30 dias
- Limite: 100 documentos, 200 req/hora
- NO requiere certificado digital (usa uno de prueba incluido)

**Flow developer**:
```
1. Developer visita /developers
2. Se registra o inicia sesion
3. Click "Crear Sandbox"
4. Recibe API key sandbox + credenciales
5. Prueba con SDK o API directa
6. Cuando esta listo → "Upgrade a Produccion" → plan pago + certificado real
```

### M4.8 — Developer Portal UI (ALTO)
**Dependencias**: M4.2, M4.7
**Archivos a crear**:
- `src/tukifact-web/src/app/developers/page.tsx` — landing developer
- `src/tukifact-web/src/app/developers/docs/page.tsx` — embed Scalar
- `src/tukifact-web/src/app/developers/quickstart/page.tsx` — guia paso a paso
- `src/tukifact-web/src/app/developers/sdks/page.tsx` — SDKs disponibles
- `src/tukifact-web/src/app/developers/sandbox/page.tsx` — crear/gestionar sandbox
- `src/tukifact-web/src/app/developers/changelog/page.tsx` — changelog API
- `src/tukifact-web/src/app/developers/status/page.tsx` — status page API

**Landing /developers**:
```
┌─────────────────────────────────────────────┐
│  TukiFact Developer Platform                │
│                                             │
│  Integra facturacion electronica SUNAT      │
│  en tu app en minutos.                      │
│                                             │
│  [Ver Documentacion]  [Crear Sandbox]       │
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  │
│  │ API Docs │  │ SDKs     │  │ Sandbox  │  │
│  │ OpenAPI  │  │ Node.js  │  │ Prueba   │  │
│  │ 3.1      │  │ Python   │  │ gratis   │  │
│  └──────────┘  └──────────┘  └──────────┘  │
│                                             │
│  Quick Start                                │
│  ─────────────                              │
│  1. npm install @tukifact/sdk               │
│  2. const tuki = new TukiFact({apiKey})     │
│  3. await tuki.documents.emit({...})        │
│                                             │
├─────────────────────────────────────────────┤
│  Planes Developer                           │
│  ┌─────────┐  ┌──────────┐  ┌───────────┐  │
│  │ Free    │  │Developer │  │ Empresa   │  │
│  │ 50/mes  │  │ 1000/mes │  │ 10000/mes │  │
│  │ $0      │  │ S/99     │  │ S/299     │  │
│  └─────────┘  └──────────┘  └───────────┘  │
└─────────────────────────────────────────────┘
```

**Quickstart page** — guia paso a paso:
```
Paso 1: Crear cuenta sandbox
  → Formulario inline o boton

Paso 2: Instalar SDK
  → Tab: Node.js | Python | cURL
  → npm install @tukifact/sdk
  → pip install tukifact

Paso 3: Emitir tu primera factura
  → Codigo ejemplo completo con output esperado
  → Boton "Ejecutar en Sandbox" (try-it-live)

Paso 4: Configurar webhooks
  → Ejemplo de como recibir notificaciones

Paso 5: Ir a produccion
  → Checklist: certificado digital, plan pago, API key live
```

**Changelog page**:
```
## v1.3.0 — 2026-04-14
- Agregado: Facturacion recurrente (POST /v1/recurring-invoices)
- Agregado: Cotizaciones con conversion a factura
- Mejorado: Rate limiting por plan

## v1.2.0 — 2026-04-13
- Agregado: Retenciones y Percepciones electronicas
- Agregado: SIRE integracion (5 endpoints)
- Agregado: Multi-moneda (USD, EUR)

## v1.1.0 — 2026-04-07
- Agregado: Guias de Remision (GRE)
- Agregado: Webhooks con retries
- Agregado: Tipo de cambio SBS automatico

## v1.0.0 — 2026-04-07
- Lanzamiento inicial
- Facturas, Boletas, NC, ND, Comunicacion de Baja, Resumen Diario
```

**Status page**:
```
API          ● Operational
Database     ● Operational
SUNAT Beta   ● Operational
SUNAT Prod   ○ Not Connected (coming soon)
MinIO        ● Operational
NATS         ● Operational

Uptime last 30 days: 99.9%
Response time (p50): 120ms
Response time (p99): 450ms
```

---

## Criterios de Completado

- [ ] OpenAPI 3.1 spec genera automaticamente desde controllers
- [ ] Scalar docs accesible en /docs con todos los endpoints documentados
- [ ] Cada endpoint tiene: descripcion, request example, response example, error codes
- [ ] SDK TypeScript publicable con tipado completo
- [ ] SDK Python publicable con tipado
- [ ] Sandbox crea tenant de prueba con API key en segundos
- [ ] Developer Portal con landing, quickstart, docs, SDKs, changelog, status
- [ ] Rate limiting diferenciado por plan
- [ ] API versioning con headers
- [ ] 0 errores lint, 0 warnings
- [ ] Build limpio frontend + backend

---

## Endpoints Documentados (checklist)

### Auth (4)
- [ ] POST /v1/auth/register
- [ ] POST /v1/auth/login
- [ ] POST /v1/auth/refresh
- [ ] POST /v1/auth/forgot-password

### Documents (6)
- [ ] POST /v1/documents/emit
- [ ] GET /v1/documents
- [ ] GET /v1/documents/{id}
- [ ] GET /v1/documents/{id}/xml
- [ ] GET /v1/documents/{id}/pdf
- [ ] GET /v1/documents/{id}/cdr

### Voided Documents (2)
- [ ] POST /v1/voided
- [ ] GET /v1/voided

### Despatch Advices (4)
- [ ] POST /v1/despatch-advices
- [ ] GET /v1/despatch-advices
- [ ] GET /v1/despatch-advices/{id}
- [ ] POST /v1/despatch-advices/{id}/emit

### Retentions (2)
- [ ] POST /v1/retentions
- [ ] GET /v1/retentions

### Perceptions (2)
- [ ] POST /v1/perceptions
- [ ] GET /v1/perceptions

### Quotations (4)
- [ ] POST /v1/quotations
- [ ] GET /v1/quotations
- [ ] GET /v1/quotations/{id}
- [ ] POST /v1/quotations/{id}/convert

### Recurring Invoices (4)
- [ ] POST /v1/recurring-invoices
- [ ] GET /v1/recurring-invoices
- [ ] PUT /v1/recurring-invoices/{id}/pause
- [ ] PUT /v1/recurring-invoices/{id}/resume

### Customers (4)
- [ ] POST /v1/customers
- [ ] GET /v1/customers
- [ ] GET /v1/customers/{id}
- [ ] PUT /v1/customers/{id}

### Products (4)
- [ ] POST /v1/products
- [ ] GET /v1/products
- [ ] GET /v1/products/{id}
- [ ] PUT /v1/products/{id}

### Series (3)
- [ ] POST /v1/series
- [ ] GET /v1/series
- [ ] DELETE /v1/series/{id}

### Webhooks (5)
- [ ] POST /v1/webhooks
- [ ] GET /v1/webhooks
- [ ] PUT /v1/webhooks/{id}
- [ ] DELETE /v1/webhooks/{id}
- [ ] GET /v1/webhooks/{id}/deliveries

### API Keys (3)
- [ ] POST /v1/api-keys
- [ ] GET /v1/api-keys
- [ ] DELETE /v1/api-keys/{id}

### Exchange Rates (2)
- [ ] GET /v1/exchange-rates
- [ ] GET /v1/exchange-rates/latest

### Catalogs (2)
- [ ] GET /v1/catalogs
- [ ] GET /v1/catalogs/{code}/items

### Dashboard (1)
- [ ] GET /v1/dashboard

### Audit Log (1)
- [ ] GET /v1/audit-log

### Lookup (2)
- [ ] GET /v1/services/lookup/ruc/{number}
- [ ] GET /v1/services/lookup/dni/{number}

### AI (2)
- [ ] POST /v1/services/ai/chat
- [ ] POST /v1/services/ai/test

### SIRE (5)
- [ ] GET /v1/sire/ventas
- [ ] GET /v1/sire/compras
- [ ] POST /v1/sire/propuesta
- [ ] GET /v1/sire/constancia
- [ ] GET /v1/sire/reporte

### Sandbox (1)
- [ ] POST /v1/sandbox/create

**Total: 56 endpoints documentados**
