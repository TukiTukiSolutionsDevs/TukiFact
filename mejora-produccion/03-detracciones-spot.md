# 03 — Sistema de Detracciones (SPOT)

> **Prioridad**: 🟡 ALTA — Obligatorio para operaciones > S/ 700 en servicios.
> **Normativa**: D.Leg. 940, R.S. 183-2004, R.S. 086-2025, R.S. 121-2025, R.S. 175-2025
> **Fuente**: orientacion.sunat.gob.pe, estudiobonilla.pe

---

## ¿Qué es?

El SPOT (Sistema de Pago de Obligaciones Tributarias) es un mecanismo de recaudación anticipada. El comprador descuenta un porcentaje del monto total y lo deposita en la cuenta del Banco de la Nación del vendedor.

## Cuándo aplica

- **Servicios**: Cuando el monto > S/ 700 (incluido IGV)
- **Bienes**: Según el bien específico (algunos desde S/ 0)
- **Transporte de carga**: Cuando el monto > S/ 400

## Catálogo 54 — Códigos de Detracción (Vigente 2025-2026)

### Anexo I — Bienes sujetos

| Código | Descripción | % |
|:------:|-------------|:-:|
| 001 | Azúcar y melaza de caña | 10% |
| 003 | Alcohol etílico | 10% |
| 004 | Recursos hidrobiológicos | 4% |
| 005 | Maíz amarillo duro | 4% |
| 007 | Caña de azúcar | 10% |
| 008 | Madera | 4% |
| 009 | Arena y piedra | 10% |
| 010 | Residuos, subproductos, desechos | 15% |
| 011 | Bienes gravados con IGV (renuncia exoneración) | 10% |
| 014 | Carnes y despojos comestibles | 4% |
| 015 | Abonos, cueros y pieles | 15% |
| 016 | Aceite de pescado | 10% |
| 017 | Harina de pescado | 4% |
| 023 | Leche | 4% |
| 024 | Páprika y otros capsicum | 10% |
| 025 | Plomo | 15% |
| 029 | Minerales metálicos no auríferos | 10% |
| 031 | Oro gravado con IGV | 10% |
| 032 | Bienes exonerados del IGV | 1.5% |
| 033 | Oro y minerales exonerados del IGV | 1.5% |
| 034 | Minerales no metálicos | 10% |

### Anexo III — Servicios sujetos

| Código | Descripción | % |
|:------:|-------------|:-:|
| 012 | Intermediación laboral y tercerización | 12% |
| 019 | Arrendamiento de bienes | 10% |
| 020 | Mantenimiento y reparación de bienes muebles | 12% |
| 021 | Movimiento de carga | 10% |
| 022 | Otros servicios empresariales | 12% |
| 024 | Comisión mercantil | 10% |
| 025 | Fabricación de bienes por encargo | 10% |
| 026 | Servicio de transporte de personas | 10% |
| 027 | Servicio de transporte de carga | 4% |
| 030 | Contratos de construcción | 4% |
| 037 | Demás servicios gravados con IGV | 12% |

## Implementación en XML UBL 2.1

### Nodos XML para detracción en Factura

```xml
<!-- 1. PaymentMeans — Información de la detracción -->
<cac:PaymentMeans>
  <cbc:ID>Detraccion</cbc:ID>
  <cbc:PaymentMeansCode>001</cbc:PaymentMeansCode> <!-- medio de pago: depósito BN -->
  <cac:PayeeFinancialAccount>
    <cbc:ID>00-123-456789</cbc:ID> <!-- cuenta Banco de la Nación -->
  </cac:PayeeFinancialAccount>
</cac:PaymentMeans>

<!-- 2. PaymentTerms — Monto y porcentaje -->
<cac:PaymentTerms>
  <cbc:ID>Detraccion</cbc:ID>
  <cbc:PaymentMeansID schemeURI="urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo54"
                       schemeName="SUNAT:Codigo de Detraccion"
                       schemeAgencyName="PE:SUNAT">037</cbc:PaymentMeansID>
  <cbc:PaymentPercent>12.00</cbc:PaymentPercent>
  <cbc:Amount currencyID="PEN">141.60</cbc:Amount>
</cac:PaymentTerms>

<!-- 3. Leyenda 2006 -->
<cbc:Note languageLocaleID="2006">Operación sujeta a detracción</cbc:Note>
```

### Leyendas XML relacionadas

| Código | Leyenda | Uso |
|:------:|---------|-----|
| 2006 | Operación sujeta a detracción | Siempre que aplique detracción |
| 3000 | Código de BB y SS sujetos a detracción | Código Cat. 54 |
| 3001 | Número de cuenta en el BN | Cuenta del vendedor |
| 3002-3009 | Datos específicos (hidrobiológicos, transporte) | Según tipo |

## Implementación en TukiFact

### Cambios en Entity Document

```csharp
// Agregar a Document.cs
public bool HasDetraction { get; set; }
public string? DetractionCode { get; set; }       // Cat. 54: "037"
public decimal? DetractionPercent { get; set; }    // 12.00
public decimal? DetractionAmount { get; set; }     // calculado
public string? DetractionBankAccount { get; set; } // cuenta BN del vendedor
```

### Entity DetractionCode (nueva)

```csharp
public class DetractionCode
{
    public string Code { get; set; }        // "037"
    public string Description { get; set; } // "Demás servicios gravados con IGV"
    public decimal Percentage { get; set; }  // 12.00
    public string Annex { get; set; }        // "I", "II", "III"
    public bool IsActive { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}
```

### Archivos a crear/modificar

| Archivo | Acción |
|---------|--------|
| `Domain/Entities/DetractionCode.cs` | NUEVO |
| `Infrastructure/Persistence/Configurations/DetractionCodeConfiguration.cs` | NUEVO |
| `Infrastructure/Services/XmlBuilders/InvoiceXmlBuilder.cs` | MODIFICAR — agregar nodos detracción |
| `API/Controllers/CatalogsController.cs` | NUEVO o MODIFICAR — endpoint detracciones |
| `Domain/Entities/Document.cs` | MODIFICAR — agregar campos detracción |
| Frontend emisión | MODIFICAR — selector detracción + cálculo auto |
| Frontend PDF | MODIFICAR — mostrar info detracción |
| Seed data SQL | NUEVO — insert todos los códigos Cat. 54 |
