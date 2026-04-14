# 01 — Guía de Remisión Electrónica (GRE)

> **Prioridad**: 🔴 CRÍTICA — Obligatoria desde enero 2024 para todos los contribuyentes.
> **Normativa**: R.S. 123-2022, R.S. 304-2024, R.S. 240-2024/SUNAT
> **Fuente**: cpe.sunat.gob.pe, Manual de servicios web GRE

---

## Contexto

La GRE sustenta el traslado de bienes dentro del territorio peruano. Todo negocio que mueva mercadería (restaurante, importador, distribuidor) NECESITA emitir guías de remisión.

### Tipos de GRE

| Tipo | Código | Serie | Emisor | Uso |
|------|:------:|-------|--------|-----|
| GRE Remitente | 09 | TXXX | El dueño de los bienes | Sustenta traslado de bienes propios |
| GRE Transportista | 31 | VXXX | La empresa de transporte | Sustenta servicio de transporte |
| GRE por Eventos | — | — | Remitente o transportista | Complementa GRE ante eventos fortuitos |

## DATO CLAVE: GRE usa REST API (NO SOAP)

A diferencia de facturas/boletas que usan SOAP, la GRE usa **API REST** con autenticación **OAuth2**.

### Flujo de autenticación

```
1. Generar credenciales en menú SOL de SUNAT
   → Obtener client_id y client_secret (una sola vez)

2. Generar token OAuth2
   POST https://api-seguridad.sunat.gob.pe/v1/clientessol/{client_id}/oauth2/token/
   Body (x-www-form-urlencoded):
     grant_type: password
     scope: https://api-cpe.sunat.gob.pe
     client_id: <client_id>
     client_secret: <client_secret>
     username: <RUC> + <usuario_sol>
     password: <clave_sol>
   
   Response:
     access_token: "eyJhb..."
     token_type: "Bearer"
     expires_in: 3600  (1 hora)

3. Enviar GRE con token
   POST https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/{numRuc}-{tipo}-{serie}-{correlativo}
   Headers:
     Authorization: Bearer <access_token>
     Content-Type: application/json
   Body: archivo ZIP con XML firmado (base64)
```

### URLs

| Ambiente | URL Token | URL Envío |
|----------|-----------|-----------|
| Beta | `https://gre-beta.sunat.gob.pe/v1/clientessol/{client_id}/oauth2/token/` | `https://gre-beta.sunat.gob.pe/v1/contribuyente/gem/comprobantes/...` |
| Producción | `https://api-seguridad.sunat.gob.pe/v1/clientessol/{client_id}/oauth2/token/` | `https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/...` |

## Estructura XML — GRE Remitente (DespatchAdvice UBL 2.1)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<DespatchAdvice xmlns="urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2"
                xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"
                xmlns:ds="http://www.w3.org/2000/09/xmldsig#"
                xmlns:ext="urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2">

  <!-- Firma digital -->
  <ext:UBLExtensions>
    <ext:UBLExtension>
      <ext:ExtensionContent>
        <ds:Signature>...</ds:Signature>
      </ext:ExtensionContent>
    </ext:UBLExtension>
  </ext:UBLExtensions>

  <cbc:UBLVersionID>2.1</cbc:UBLVersionID>
  <cbc:CustomizationID>2.0</cbc:CustomizationID>

  <!-- Serie-Correlativo -->
  <cbc:ID>T001-00000001</cbc:ID>

  <!-- Fecha y hora emisión -->
  <cbc:IssueDate>2026-04-13</cbc:IssueDate>
  <cbc:IssueTime>10:30:00</cbc:IssueTime>

  <!-- Tipo documento: 09 = GRE Remitente -->
  <cbc:DespatchAdviceTypeCode>09</cbc:DespatchAdviceTypeCode>

  <!-- Observaciones -->
  <cbc:Note>Traslado de mercadería</cbc:Note>

  <!-- Documento relacionado (factura, boleta, etc.) -->
  <cac:OrderReference>
    <cbc:ID>F001-00000123</cbc:ID>
    <cbc:OrderTypeCode>01</cbc:OrderTypeCode> <!-- tipo doc relacionado -->
  </cac:OrderReference>

  <!-- Datos del remitente (emisor) -->
  <cac:DespatchSupplierParty>
    <cbc:CustomerAssignedAccountID>20613614509</cbc:CustomerAssignedAccountID>
    <cac:Party>
      <cac:PartyLegalEntity>
        <cbc:RegistrationName>TUKITUKI SOLUTION SAC</cbc:RegistrationName>
      </cac:PartyLegalEntity>
    </cac:Party>
  </cac:DespatchSupplierParty>

  <!-- Datos del destinatario -->
  <cac:DeliveryCustomerParty>
    <cbc:CustomerAssignedAccountID>20100047218</cbc:CustomerAssignedAccountID>
    <cac:Party>
      <cac:PartyLegalEntity>
        <cbc:RegistrationName>SODIMAC PERU S.A.</cbc:RegistrationName>
      </cac:PartyLegalEntity>
    </cac:Party>
  </cac:DeliveryCustomerParty>

  <!-- Datos del envío -->
  <cac:Shipment>
    <cbc:ID>1</cbc:ID>
    <cbc:HandlingCode>01</cbc:HandlingCode> <!-- Cat. 20: motivo traslado -->
    <cbc:HandlingInstructions>Venta</cbc:HandlingInstructions>
    <cbc:GrossWeightMeasure unitCode="KGM">150.00</cbc:GrossWeightMeasure>
    <cbc:TotalTransportHandlingUnitQuantity>5</cbc:TotalTransportHandlingUnitQuantity>

    <!-- Modalidad de transporte: 01=Público, 02=Privado -->
    <cac:ShipmentStage>
      <cbc:TransportModeCode>01</cbc:TransportModeCode>
      <cac:TransitPeriod>
        <cbc:StartDate>2026-04-14</cbc:StartDate>
      </cac:TransitPeriod>
      <!-- Datos del transportista (si transporte público) -->
      <cac:CarrierParty>
        <cac:PartyIdentification>
          <cbc:ID schemeID="6">20100090235</cbc:ID>
        </cac:PartyIdentification>
        <cac:PartyLegalEntity>
          <cbc:RegistrationName>TRANSPORTES CRUZ DEL SUR SAC</cbc:RegistrationName>
        </cac:PartyLegalEntity>
      </cac:CarrierParty>
    </cac:ShipmentStage>

    <!-- Dirección de partida -->
    <cac:DeliveryAddress>
      <cbc:ID>150101</cbc:ID> <!-- UBIGEO destino -->
      <cac:AddressLine>
        <cbc:Line>Av. Los Frutales 123, Ate</cbc:Line>
      </cac:AddressLine>
    </cac:DeliveryAddress>

    <!-- Dirección de origen -->
    <cac:OriginAddress>
      <cbc:ID>040601</cbc:ID> <!-- UBIGEO origen (Arequipa-Cayma) -->
      <cac:AddressLine>
        <cbc:Line>Calle Ejemplo 456, Cayma</cbc:Line>
      </cac:AddressLine>
    </cac:OriginAddress>
  </cac:Shipment>

  <!-- Items (bienes trasladados) -->
  <cac:DespatchLine>
    <cbc:ID>1</cbc:ID>
    <cbc:DeliveredQuantity unitCode="NIU">50</cbc:DeliveredQuantity>
    <cac:Item>
      <cbc:Name>Caja de productos alimenticios</cbc:Name>
      <cac:SellersItemIdentification>
        <cbc:ID>PROD-001</cbc:ID>
      </cac:SellersItemIdentification>
    </cac:Item>
  </cac:DespatchLine>
</DespatchAdvice>
```

## Catálogo 20 — Motivos de Traslado

| Código | Descripción |
|:------:|-------------|
| 01 | Venta |
| 02 | Compra |
| 03 | Venta con entrega a terceros |
| 04 | Traslado entre establecimientos de la misma empresa |
| 05 | Consignación |
| 06 | Devolución |
| 07 | Recojo de bienes transformados |
| 08 | Importación |
| 09 | Exportación |
| 10 | Venta sujeta a confirmación del comprador |
| 11 | Traslado de zona primaria |
| 12 | Traslado emisor itinerante de CP |
| 13 | Traslado a zona primaria |
| 14 | Otros |
| 17 | Traslado de bienes para transformación |
| 18 | Recojo de bienes |
| 19 | Traslado por emisor itinerante de CP |

## Implementación en TukiFact

### Entity: DespatchAdvice

```csharp
public class DespatchAdvice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    // Documento
    public string DocumentType { get; set; } // "09" o "31"
    public string Serie { get; set; } // T001, V001
    public int Correlative { get; set; }
    public string FullNumber => $"{Serie}-{Correlative:D8}";
    
    // Fechas
    public DateOnly IssueDate { get; set; }
    public TimeOnly IssueTime { get; set; }
    public DateOnly TransferStartDate { get; set; }
    
    // Motivo
    public string TransferReasonCode { get; set; } // Cat. 20
    public string TransferReasonDescription { get; set; }
    public string? Note { get; set; }
    
    // Peso y unidades
    public decimal GrossWeight { get; set; }
    public string WeightUnitCode { get; set; } = "KGM";
    public int TotalPackages { get; set; }
    
    // Transporte
    public string TransportMode { get; set; } // "01"=Público, "02"=Privado
    
    // Transportista (si transporte público)
    public string? CarrierDocType { get; set; }
    public string? CarrierDocNumber { get; set; }
    public string? CarrierName { get; set; }
    public string? CarrierMtcNumber { get; set; }
    
    // Conductor (si transporte privado)
    public string? DriverDocType { get; set; }
    public string? DriverDocNumber { get; set; }
    public string? DriverName { get; set; }
    public string? DriverLicense { get; set; }
    
    // Vehículo
    public string? VehiclePlate { get; set; }
    public string? SecondaryVehiclePlate { get; set; }
    
    // Destinatario
    public string RecipientDocType { get; set; }
    public string RecipientDocNumber { get; set; }
    public string RecipientName { get; set; }
    
    // Direcciones
    public string OriginUbigeo { get; set; }
    public string OriginAddress { get; set; }
    public string DestinationUbigeo { get; set; }
    public string DestinationAddress { get; set; }
    
    // Documento relacionado
    public string? RelatedDocType { get; set; }
    public string? RelatedDocNumber { get; set; }
    
    // Estado SUNAT
    public string SunatStatus { get; set; } = "pending"; // pending, accepted, rejected
    public string? SunatResponseCode { get; set; }
    public string? SunatResponseMessage { get; set; }
    public string? SunatTicket { get; set; }
    
    // Archivos
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? CdrUrl { get; set; }
    
    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    
    // Navegación
    public List<DespatchAdviceItem> Items { get; set; } = new();
}
```

### Entity: DespatchAdviceItem

```csharp
public class DespatchAdviceItem
{
    public Guid Id { get; set; }
    public Guid DespatchAdviceId { get; set; }
    
    public int LineNumber { get; set; }
    public string Description { get; set; }
    public string? ProductCode { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; } = "NIU";
    
    public DespatchAdvice DespatchAdvice { get; set; }
}
```

### Archivos a crear

| Capa | Archivo | Descripción |
|------|---------|-------------|
| Domain | `Entities/DespatchAdvice.cs` | Entity principal |
| Domain | `Entities/DespatchAdviceItem.cs` | Items |
| Domain | `Entities/Ubigeo.cs` | Tabla UBIGEO (1874 distritos) |
| Infrastructure | `Persistence/Configurations/DespatchAdviceConfiguration.cs` | EF Config |
| Infrastructure | `Persistence/Configurations/UbigeoConfiguration.cs` | EF Config |
| Infrastructure | `Services/GreXmlBuilder.cs` | Genera XML UBL 2.1 DespatchAdvice |
| Infrastructure | `Services/GreSunatClient.cs` | REST client OAuth2 para envío |
| Infrastructure | `Services/SunatOAuth2Service.cs` | Token service reutilizable |
| API | `Controllers/DespatchAdviceController.cs` | CRUD + envío |
| Frontend | `app/(authenticated)/gre/page.tsx` | Lista GRE |
| Frontend | `app/(authenticated)/gre/new/page.tsx` | Emisión GRE |
| Frontend | `app/(authenticated)/gre/[id]/page.tsx` | Detalle GRE |

### Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `AppDbContext.cs` | Agregar `DbSet<DespatchAdvice>`, `DbSet<Ubigeo>` |
| Sidebar frontend | Agregar ruta GRE |
| `docker/postgres/init/` | Seed UBIGEO + RLS para despatch_advices |

### Dependencias UBIGEO

Se necesita tabla UBIGEO completa con 1874 distritos. Estructura:

```sql
CREATE TABLE ubigeo (
    code VARCHAR(6) PRIMARY KEY,      -- "040601"
    department VARCHAR(50) NOT NULL,   -- "AREQUIPA"
    province VARCHAR(50) NOT NULL,     -- "AREQUIPA"
    district VARCHAR(50) NOT NULL,     -- "CAYMA"
    is_active BOOLEAN DEFAULT true
);
```

Seed: descargar del INEI o usar CSV público.

### Referencia: thegreenter/gre-api

Librería PHP open source que implementa GRE REST API para SUNAT:
- GitHub: `github.com/thegreenter/gre-api`
- Tiene `openapi.yaml` con toda la especificación REST
- Útil como referencia para implementar el cliente .NET
