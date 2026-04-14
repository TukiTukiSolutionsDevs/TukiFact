# 02 — SUNAT Producción Real

> **Prioridad**: 🔴 CRÍTICA — Sin esto todo es juego, no se puede facturar de verdad.
> **Fuente**: cpe.sunat.gob.pe, Manual del Programador SUNAT

---

## URLs de Servicios Web SUNAT

### SOAP — Comprobantes de Pago (Factura, Boleta, NC, ND)

| Ambiente | URL |
|----------|-----|
| **Beta** | `https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService` |
| **Producción** | `https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService` |

**Métodos SOAP disponibles**:
- `sendBill` — Envía ZIP con XML, devuelve CDR (síncrono)
- `sendSummary` — Envía resumen diario/comunicación de baja, devuelve ticket (asíncrono)
- `getStatus` — Consulta estado de ticket

### SOAP — Retenciones y Percepciones

| Ambiente | URL |
|----------|-----|
| **Beta** | `https://e-beta.sunat.gob.pe/ol-ti-itemision-otroscpe-gem-beta/billService` |
| **Producción** | `https://e-factura.sunat.gob.pe/ol-ti-itemision-otroscpe-gem/billService` |

### REST — Guía de Remisión Electrónica (GRE)

| Ambiente | URL Token | URL Envío |
|----------|-----------|-----------|
| **Beta** | `https://gre-beta.sunat.gob.pe/v1/clientessol/{client_id}/oauth2/token/` | `https://gre-beta.sunat.gob.pe/v1/contribuyente/gem/comprobantes/...` |
| **Producción** | `https://api-seguridad.sunat.gob.pe/v1/clientessol/{client_id}/oauth2/token/` | `https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/...` |

### REST — Consulta de CDR y Estado

| URL | Descripción |
|-----|-------------|
| `https://e-factura.sunat.gob.pe/ol-it-wsconscpegem/billConsultService` | Consulta CDR por número |

### REST — Consulta Integrada de Validez CPE

Requiere credenciales de API SUNAT (generar en SOL → Credenciales de API SUNAT):
- Generar `client_id` y `client_secret`
- Obtener token OAuth2
- Consultar validez de comprobante

### REST — SIRE (Registro Ventas/Compras)

| URL Token | `https://api-seguridad.sunat.gob.pe/v1/clientessol/{client_id}/oauth2/token/` |
|-----------|---|
| URL Base SIRE | `https://api-sire.sunat.gob.pe/...` |

## Autenticación SOAP

```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                  xmlns:ser="http://service.sunat.gob.pe"
                  xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
  <soapenv:Header>
    <wsse:Security>
      <wsse:UsernameToken>
        <wsse:Username>20613614509MODDATOS</wsse:Username>
        <wsse:Password>moddatos</wsse:Password>
      </wsse:UsernameToken>
    </wsse:Security>
  </soapenv:Header>
  <soapenv:Body>
    <ser:sendBill>
      <fileName>20613614509-01-F001-1.zip</fileName>
      <contentFile>BASE64_ZIP_CONTENT</contentFile>
    </ser:sendBill>
  </soapenv:Body>
</soapenv:Envelope>
```

**Nota**: En producción, Username = `{RUC}{UsuarioSOL}` y Password = `{ClaveSOL}`.

## Implementación: Switch Beta/Producción por Tenant

### Tabla tenant_service_config (ya existe)

Agregar campos:

```csharp
// En TenantServiceConfig o nueva tabla
public string SunatEnvironment { get; set; } = "beta"; // "beta" | "production"

// Credenciales SOAP (factura/boleta)
public string? SunatSolUser { get; set; }      // usuario SOL
public string? SunatSolPassword { get; set; }  // clave SOL (encriptada)

// Credenciales REST (GRE, SIRE, consulta)
public string? SunatApiClientId { get; set; }
public string? SunatApiClientSecret { get; set; }

// Certificado digital
public string? CertificatePath { get; set; }   // path en MinIO
public string? CertificatePassword { get; set; } // encriptada
```

### SunatClientFactory

```csharp
public interface ISunatClientFactory
{
    ISunatSoapClient CreateSoapClient(Guid tenantId);
    ISunatRestClient CreateRestClient(Guid tenantId);
}
```

Resuelve URLs y credenciales según `SunatEnvironment` del tenant.

## Proceso de Homologación SUNAT

Para pasar de beta a producción, SUNAT requiere un **set de pruebas**:

1. Emitir factura gravada
2. Emitir factura exonerada
3. Emitir factura inafecta
4. Emitir factura de exportación
5. Emitir boleta
6. Emitir nota de crédito
7. Emitir nota de débito
8. Generar resumen diario
9. Enviar comunicación de baja
10. Consultar CDR

Cada documento debe ser aceptado por SUNAT beta. Una vez aprobado, se habilita producción.

## Archivos a Crear/Modificar

| Archivo | Acción |
|---------|--------|
| `Services/SunatClientFactory.cs` | NUEVO — Factory que resuelve URLs por tenant |
| `Services/SunatSoapClient.cs` | MODIFICAR — Parametrizar URL (no hardcoded) |
| `Services/SunatOAuth2Service.cs` | NUEVO — Token service para REST APIs |
| `Controllers/CertificateController.cs` | MODIFICAR — Agregar subida de .pfx real |
| Configuración tenant (frontend) | MODIFICAR — Campos SOL user/password, API credentials |
| `AppDbContext.cs` | MODIFICAR — Nuevos campos en TenantServiceConfig |

## Seguridad

- Credenciales SOL y API SUNAT se guardan **encriptadas** en DB (AES-256 o DataProtection)
- Certificados .pfx se guardan en MinIO bucket `tukifact-certs` (encriptados)
- Nunca exponer credenciales SUNAT en logs
- Rotar certificados sin downtime
