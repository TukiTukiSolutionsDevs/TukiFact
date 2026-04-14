# 07 — Comprobantes de Retención y Percepción Electrónicos

> **Prioridad**: 🟠 MEDIA — Necesario para empresas designadas como agentes.
> **Normativa**: R.S. 274-2015 (Retención), R.S. 199-2019 (Percepción)
> **Fuente**: cpe.sunat.gob.pe, Manual del Programador SUNAT

---

## Retención Electrónica (Tipo 20)

### ¿Qué es?
Documento emitido por el **agente de retención** (designado por SUNAT) al proveedor. Acredita la retención del 3% del IGV sobre el monto pagado.

### Datos clave

| Campo | Valor |
|-------|-------|
| Tipo documento | 20 |
| Serie | R001, R002... |
| Porcentaje | 3% del monto pagado |
| Emisor | Agente de retención (comprador) |
| XML estándar | UBL 2.0 (NO 2.1 — diferente a facturas) |
| Endpoint SOAP | Separado del de facturas |

### Estructura XML (simplificada)

```xml
<Retention xmlns="urn:sunat:names:specification:ubl:peru:schema:xsd:Retention-1">
  <cbc:UBLVersionID>2.0</cbc:UBLVersionID>
  <cbc:ID>R001-00000001</cbc:ID>
  <cbc:IssueDate>2026-04-13</cbc:IssueDate>
  
  <!-- Agente de retención (emisor) -->
  <cac:AgentParty>...</cac:AgentParty>
  
  <!-- Proveedor (receptor de la retención) -->
  <cac:ReceiverParty>...</cac:ReceiverParty>
  
  <!-- Régimen de retención -->
  <sac:SUNATRetentionSystemCode>01</sac:SUNATRetentionSystemCode>
  <sac:SUNATRetentionPercent>3.00</sac:SUNATRetentionPercent>
  
  <!-- Total retenido -->
  <cbc:TotalInvoiceAmount currencyID="PEN">1000.00</cbc:TotalInvoiceAmount>
  <sac:SUNATTotalPaid currencyID="PEN">970.00</sac:SUNATTotalPaid>
  <sac:SUNATTotalCashed currencyID="PEN">30.00</sac:SUNATTotalCashed>
  
  <!-- Documentos relacionados -->
  <sac:SUNATRetentionDocumentReference>
    <cbc:ID>F001-00000100</cbc:ID>
    <cbc:IssueDate>2026-04-01</cbc:IssueDate>
    <cbc:TotalInvoiceAmount currencyID="PEN">1000.00</cbc:TotalInvoiceAmount>
    <cac:Payment>
      <cbc:PaidAmount currencyID="PEN">1000.00</cbc:PaidAmount>
      <cbc:PaidDate>2026-04-10</cbc:PaidDate>
    </cac:Payment>
    <sac:SUNATRetentionAmount currencyID="PEN">30.00</sac:SUNATRetentionAmount>
    <sac:SUNATNetTotalPaid currencyID="PEN">970.00</sac:SUNATNetTotalPaid>
  </sac:SUNATRetentionDocumentReference>
</Retention>
```

## Percepción Electrónica (Tipo 40)

### ¿Qué es?
Documento emitido por el **agente de percepción** (vendedor designado por SUNAT) al comprador. El vendedor cobra un porcentaje adicional sobre el precio de venta.

### Datos clave

| Campo | Valor |
|-------|-------|
| Tipo documento | 40 |
| Serie | P001, P002... |
| Porcentajes | 0.5%, 1%, 2% según caso (Cat. 22) |
| Emisor | Agente de percepción (vendedor) |
| XML estándar | UBL 2.0 |
| Endpoint SOAP | Mismo que retención |

### Catálogo 22 — Regímenes de Percepción

| Código | Descripción | % |
|:------:|-------------|:-:|
| 01 | Percepción venta interna | 2% |
| 02 | Percepción adquisición de combustible | 1% |
| 03 | Percepción operación por la que se emite CdP | 0.5% |

### Endpoint SOAP (Beta y Producción)

| Ambiente | URL |
|----------|-----|
| Beta | `https://e-beta.sunat.gob.pe/ol-ti-itemision-otroscpe-gem-beta/billService` |
| Producción | `https://e-factura.sunat.gob.pe/ol-ti-itemision-otroscpe-gem/billService` |

**Mismo método**: `sendBill` (idéntico al de facturas, diferente URL).

## Implementación en TukiFact

### Entities

```csharp
public class RetentionDocument
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Serie { get; set; } // R001
    public int Correlative { get; set; }
    public DateOnly IssueDate { get; set; }
    
    // Proveedor (a quien se retiene)
    public string SupplierDocType { get; set; }
    public string SupplierDocNumber { get; set; }
    public string SupplierName { get; set; }
    
    // Régimen
    public string RegimeCode { get; set; } = "01";
    public decimal RetentionPercent { get; set; } = 3.00m;
    
    // Totales
    public decimal TotalInvoiceAmount { get; set; }
    public decimal TotalRetained { get; set; }
    public decimal TotalPaid { get; set; }
    public string Currency { get; set; } = "PEN";
    
    // SUNAT
    public string SunatStatus { get; set; } = "pending";
    public string? SunatResponseCode { get; set; }
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }
    
    public List<RetentionDocumentReference> References { get; set; } = new();
}

public class RetentionDocumentReference
{
    public Guid Id { get; set; }
    public Guid RetentionDocumentId { get; set; }
    public string DocumentType { get; set; } // "01" factura
    public string DocumentNumber { get; set; } // "F001-00000100"
    public DateOnly DocumentDate { get; set; }
    public decimal InvoiceAmount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal RetainedAmount { get; set; }
    public decimal NetPaid { get; set; }
}
```

### Archivos a crear

| Archivo | Descripción |
|---------|-------------|
| `Domain/Entities/RetentionDocument.cs` | Entity retención |
| `Domain/Entities/PerceptionDocument.cs` | Entity percepción (similar) |
| `Infrastructure/Services/RetentionXmlBuilder.cs` | XML UBL 2.0 |
| `Infrastructure/Services/PerceptionXmlBuilder.cs` | XML UBL 2.0 |
| `API/Controllers/RetentionsController.cs` | CRUD + envío |
| `API/Controllers/PerceptionsController.cs` | CRUD + envío |
| Frontend retenciones | Emisión + lista |
| Frontend percepciones | Emisión + lista |

### Nota importante

Retenciones y percepciones usan **UBL 2.0** (no 2.1). Los namespaces XML son diferentes:
- `urn:sunat:names:specification:ubl:peru:schema:xsd:Retention-1`
- `urn:sunat:names:specification:ubl:peru:schema:xsd:Perception-1`

Esto requiere XML builders separados, no reutilizar el de facturas.
