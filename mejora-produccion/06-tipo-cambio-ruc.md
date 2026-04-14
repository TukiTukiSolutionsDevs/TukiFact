# 06 — Tipo de Cambio SUNAT + Validar RUC/DNI

> **Prioridad**: 🟡 ALTA (RUC) / MEDIA (Tipo cambio)
> **Fuente**: apis.net.pe, peruapi.com, ww3.sunat.gob.pe

---

## A. Validar RUC

### APIs disponibles

| API | URL | Costo | Respuesta |
|-----|-----|-------|-----------|
| apis.net.pe | `GET /v2/sunat/ruc?numero={ruc}` | Freemium (100/día gratis) | razón social, estado, condición, dirección, ubigeo |
| peruapi.com | `GET /api/ruc/{ruc}?api_token=KEY` | Freemium | similar |
| SUNAT directo | Scraping ww1.sunat.gob.pe | Gratis pero frágil | razón social, estado |

### Ejemplo respuesta apis.net.pe

```json
{
  "ruc": "20100047218",
  "razonSocial": "SODIMAC PERU S.A.",
  "estado": "ACTIVO",
  "condicion": "HABIDO",
  "direccion": "AV. ANGAMOS ESTE NRO. 1805",
  "ubigeo": "150130",
  "departamento": "LIMA",
  "provincia": "LIMA",
  "distrito": "SURQUILLO"
}
```

### Implementación

```csharp
public interface IRucValidationService
{
    Task<RucInfo?> ValidateRuc(string ruc);
    Task<DniInfo?> ValidateDni(string dni);
}
```

**Endpoints**:
- `GET /v1/utils/validate-ruc/{ruc}` — Valida RUC, devuelve datos
- `GET /v1/utils/validate-dni/{dni}` — Valida DNI, devuelve nombres

**Cache**: Guardar en tabla `ruc_cache` por 24h (evitar hits repetidos).

**Frontend**: En formulario de emisión y gestión de clientes, al escribir 11 dígitos de RUC → autocompletar razón social, dirección, ubigeo.

## B. Validar DNI

Mismas APIs:
- `GET /v2/sunat/dni?numero={dni}` (apis.net.pe)
- `GET /api/dni/{dni}?api_token=KEY` (peruapi.com)

Respuesta: nombres, apellido paterno, apellido materno.

## C. Tipo de Cambio SUNAT

### Fuente

SUNAT publica el tipo de cambio diario basado en la cotización de cierre de la SBS del día anterior.

### APIs

| API | URL | Respuesta |
|-----|-----|-----------|
| apis.net.pe | `GET /v2/sunat/tipo-cambio?fecha=YYYY-MM-DD` | compra, venta, fecha |
| peruapi.com | `GET /api/tipo_cambio?fecha=YYYY-MM-DD&api_token=KEY` | compra, venta |
| SUNAT directo | `ww3.sunat.gob.pe/cl-ad-ittipocambioconsulta/...` | HTML (scraping) |

### Ejemplo respuesta

```json
{
  "fecha": "2026-04-13",
  "compra": "3.519",
  "venta": "3.527",
  "moneda": "USD",
  "fuente": "SUNAT"
}
```

### Implementación

```csharp
public class ExchangeRate
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
    public string Source { get; set; } = "SBS";
    public DateTime FetchedAt { get; set; }
}
```

**Endpoint**: `GET /v1/utils/exchange-rate?date=2026-04-13&currency=USD`

**Scheduler**: Background service que actualiza tipo de cambio cada día a las 6:00 PM (después del cierre SBS).

**Frontend**: Al seleccionar moneda USD en emisión, mostrar tipo de cambio automático y calcular equivalente PEN.

### Archivos a crear

| Archivo | Descripción |
|---------|-------------|
| `Domain/Entities/ExchangeRate.cs` | Entity |
| `Infrastructure/Services/RucValidationService.cs` | Consulta RUC/DNI |
| `Infrastructure/Services/ExchangeRateService.cs` | Tipo de cambio |
| `API/Controllers/UtilsController.cs` | Endpoints utilitarios |
| Frontend emisión | Autocompletar RUC + tipo de cambio |
