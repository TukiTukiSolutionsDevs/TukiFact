# 09 — Funcionalidades Complementarias

> Rate limiting, forgot password, catálogos SUNAT, multi-moneda, facturación recurrente, cotizaciones.

---

## A. Rate Limiting por Plan

### Límites por plan

| Plan | Docs/mes | API requests/min |
|------|:--------:|:----------------:|
| Free | 50 | 10 |
| Emprendedor | 300 | 30 |
| Negocio | 1,000 | 60 |
| Developer | 1,000 | 100 |
| Profesional | 3,000 | 100 |
| Empresa | 10,000 | 300 |

### Implementación
- Middleware que cuenta docs emitidos por tenant/mes
- PostgreSQL counter para MVP, Redis a escala
- Headers: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`
- Endpoint `GET /v1/usage` — uso actual del plan
- Frontend: barra de progreso en dashboard

---

## B. Forgot / Reset Password

### Flujo
1. `POST /v1/auth/forgot-password { email }` → genera token UUID + envía email
2. `POST /v1/auth/reset-password { token, newPassword }` → valida token (1h TTL) + cambia password

### Entity PasswordResetToken
```csharp
public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? IpAddress { get; set; }
}
```

**Dependencia**: Email service (spec 08)

---

## C. Catálogos SUNAT API

### Catálogos prioritarios
01 (Tipo doc), 02 (Moneda), 03 (Unidad medida), 05 (Tributos), 06 (Doc identidad), 07 (Afectación IGV), 09 (Motivo NC), 10 (Motivo ND), 13 (UBIGEO), 20 (Motivo traslado), 22 (Percepción), 51 (Tipo operación), 53 (Cargos/descuentos), 54 (Detracción)

### Endpoints
```
GET /v1/catalogs                       — Lista catálogos
GET /v1/catalogs/{number}              — Detalle
GET /v1/catalogs/{number}/codes        — Códigos
GET /v1/catalogs/{number}/codes/{code} — Código específico
```

### Entities
```csharp
public class SunatCatalog
{
    public int Number { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<SunatCatalogCode> Codes { get; set; } = new();
}

public class SunatCatalogCode
{
    public int Id { get; set; }
    public int CatalogNumber { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; } = true;
}
```

---

## D. Multi-moneda (USD/EUR)

Campo `currency` ya existe en Document. Falta:

### XML PaymentExchangeRate
```xml
<cbc:DocumentCurrencyCode>USD</cbc:DocumentCurrencyCode>
<cac:PaymentExchangeRate>
  <cbc:SourceCurrencyCode>USD</cbc:SourceCurrencyCode>
  <cbc:TargetCurrencyCode>PEN</cbc:TargetCurrencyCode>
  <cbc:CalculationRate>3.527</cbc:CalculationRate>
  <cbc:Date>2026-04-13</cbc:Date>
</cac:PaymentExchangeRate>
```

- Integrar tipo de cambio automático (spec 06)
- Frontend: selector moneda + tipo de cambio auto
- PDF: mostrar ambas monedas

---

## E. Facturación Recurrente (DIFERENCIADOR — Nubefact NO lo tiene)

### Entity
```csharp
public class RecurringInvoice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string DocumentType { get; set; }
    public string CustomerDocNumber { get; set; }
    public string CustomerName { get; set; }
    public string ItemsJson { get; set; } // template items
    public string Frequency { get; set; } // daily/weekly/biweekly/monthly/yearly
    public int? DayOfMonth { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? NextEmissionDate { get; set; }
    public string Status { get; set; } = "active"; // active/paused/cancelled
    public int EmittedCount { get; set; }
}
```

### Scheduler
IHostedService que cada hora revisa `next_emission_date <= today`, genera documento, actualiza fecha, envía email.

---

## F. Cotizaciones → Factura (DIFERENCIADOR)

### Entity
```csharp
public class Quotation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string QuotationNumber { get; set; } // COT-001
    public DateOnly IssueDate { get; set; }
    public DateOnly ValidUntil { get; set; }
    public string CustomerDocNumber { get; set; }
    public string CustomerName { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "draft";
    // draft → sent → approved → invoiced → cancelled
    public Guid? InvoiceDocumentId { get; set; }
    public List<QuotationItem> Items { get; set; } = new();
}
```

### Flujo
Crear cotización → enviar PDF por email → cliente aprueba → click "Convertir a Factura" → genera factura automáticamente con los mismos datos.

---

## Archivos totales a crear

| Archivo | Funcionalidad |
|---------|---------------|
| `API/Middleware/RateLimitMiddleware.cs` | Rate limiting |
| `Domain/Entities/PasswordResetToken.cs` | Reset password |
| `Domain/Entities/SunatCatalog.cs` | Catálogos |
| `Domain/Entities/SunatCatalogCode.cs` | Catálogos |
| `Domain/Entities/RecurringInvoice.cs` | Facturación recurrente |
| `Domain/Entities/Quotation.cs` | Cotizaciones |
| `Domain/Entities/QuotationItem.cs` | Cotizaciones |
| `API/Controllers/CatalogsController.cs` | Catálogos SUNAT |
| `API/Controllers/QuotationsController.cs` | Cotizaciones |
| `API/Controllers/RecurringInvoicesController.cs` | Recurrente |
| `Infrastructure/Services/RecurringInvoiceScheduler.cs` | Scheduler |
| `Infrastructure/Services/QuotationPdfGenerator.cs` | PDF cotización |
| Seed SQL catálogos | Insert todos los códigos |
| Frontend: 6+ páginas nuevas | Todas las funcionalidades |
