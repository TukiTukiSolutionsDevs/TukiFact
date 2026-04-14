# Mega-Sprint 2: SUNAT Produccion Real

> **Tema**: Cliente SUNAT produccion + Homologacion + Error handling
> **Estimacion**: ~3-4 dias
> **Prioridad**: CRITICA — sin esto NO facturas de verdad

---

## Contexto

Ya existe:
- `SunatClient.cs` — cliente SOAP con modo beta/stub (simula aceptacion)
- `GreSunatClient.cs` — cliente GRE (guias remision)
- `UblBuilder.cs` — genera XML UBL 2.1 compliant
- `XmlSigningService.cs` — firma digital X509
- `CdrParser.cs` — parsea CDR (Constancia de Recepcion)
- `CpeValidationService.cs` — consulta validez CPE
- `CertificateController.cs` — upload certificado .pfx

Lo que FALTA: conectar todo contra SUNAT real y manejar los errores reales.

---

## Tareas

### M2.1 — SunatClient Modo Produccion (ALTO)
**Dependencias**: ninguna
**Archivos a modificar**:
- `src/TukiFact.Infrastructure/Services/SunatClient.cs`
- `src/TukiFact.Api/appsettings.json` — config URLs
- `src/TukiFact.Domain/Entities/Tenant.cs` — flag `SunatMode` (beta/produccion)

**URLs SUNAT reales**:
```
# Beta (pruebas)
sendBill:    https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService
getStatus:   https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService

# Produccion
sendBill:    https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService
getStatus:   https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService

# Resumen/Baja
sendSummary: https://e-factura.sunat.gob.pe/ol-ti-itcpgem-sfs/billService
```

**Cambios**:
```csharp
// Nuevo: modo por tenant
public enum SunatMode { Beta, Production }

// SunatClient recibe mode del tenant:
public async Task<SunatResponse> SendBillAsync(
    byte[] zipFile, string fileName, SunatMode mode, SunatCredentials creds, CancellationToken ct)
{
    var url = mode == SunatMode.Production
        ? _config.ProductionUrl
        : _config.BetaUrl;
    // ... SOAP call real
}
```

**Credenciales por tenant** (ya en Tenant entity):
- SOL User (ej: 20613614509MODDATOS)
- SOL Password
- Certificado .pfx (en MinIO)
- Clave del certificado

### M2.2 — Certificado Digital Real UI (MEDIO)
**Dependencias**: ninguna
**Archivos a modificar**:
- `src/TukiFact.Api/Controllers/CertificateController.cs` — validar .pfx real
- `src/tukifact-web/src/app/(authenticated)/certificate/page.tsx` — mejorar UI

**Flujo upload mejorado**:
```
1. Usuario sube .pfx + ingresa password
2. Backend valida:
   - Es un PKCS12 valido
   - Tiene clave privada
   - No esta expirado
   - Subject contiene RUC del tenant
3. Si ok: almacena en MinIO bucket "certs" (encriptado)
4. Muestra: emisor, vigencia, serial number, thumbprint
5. Warning si expira en <30 dias
```

### M2.3 — Homologacion Set de Pruebas (ALTO)
**Dependencias**: M2.1
**Archivos a crear**:
- `src/TukiFact.Api/Controllers/HomologacionController.cs`
- `src/TukiFact.Infrastructure/Services/HomologacionService.cs`
- `src/tukifact-web/src/app/(authenticated)/homologacion/page.tsx`

**Set de pruebas SUNAT** (~25 documentos):
```
FACTURAS (01):
  1. Factura gravada (IGV 18%)
  2. Factura exonerada
  3. Factura inafecta
  4. Factura gratuita
  5. Factura exportacion
  6. Factura con detraccion
  7. Factura con descuento global
  8. Factura con anticipos

BOLETAS (03):
  9. Boleta gravada
  10. Boleta exonerada

NOTAS CREDITO (07):
  11. NC por anulacion
  12. NC por descuento
  13. NC por devolucion

NOTAS DEBITO (08):
  14. ND por intereses
  15. ND por penalidad

COMUNICACION BAJA (RA):
  16. Baja de factura
  17. Baja de boleta

RESUMEN DIARIO (RC):
  18. Resumen con boletas

GUIAS REMISION (T09):
  19. GRE remitente
  20. GRE transportista

RETENCIONES (20):
  21. Retencion tasa 3%

PERCEPCIONES (40):
  22. Percepcion tasa 2%
```

**UI Homologacion**:
- Lista de los ~22 documentos requeridos
- Boton "Generar y Enviar" por cada uno
- Estado: pendiente/enviado/aceptado/rechazado
- Progreso general: X/22 completados
- Al completar todos: "LISTO PARA PRODUCCION"

### M2.4 — Consulta Validez CPE Frontend (BAJO)
**Dependencias**: M2.1
**Archivos a modificar**:
- `src/tukifact-web/src/app/(authenticated)/documents/[id]/page.tsx` — boton "Verificar en SUNAT"

**Que hace**:
- Boton en detalle de documento: "Verificar en SUNAT"
- Llama a CpeValidationService
- Muestra estado real de SUNAT: Aceptado / Rechazado / No encontrado
- Badge de estado actualizado

### M2.5 — Error Handling SUNAT (MEDIO)
**Dependencias**: M2.1
**Archivos a modificar**:
- `src/TukiFact.Infrastructure/Services/SunatClient.cs` — manejo errores
- `src/TukiFact.Infrastructure/Services/CdrParser.cs` — mas codigos
- `src/TukiFact.Domain/Entities/Document.cs` — campos error

**Codigos de rechazo comunes SUNAT**:
```
0    → Aceptado
100+ → Excepciones (rechazado)
  2015 → RUC emisor no valido
  2017 → Tipo documento no valido para serie
  2022 → Fecha emision invalida
  2033 → Moneda invalida
  2070 → Serie no corresponde al tipo
  3000+ → Observaciones (aceptado con observaciones)
  4000+ → Informativas
```

**Estados del documento mejorados**:
```
draft → pending → sending → sent → accepted → accepted_with_observations → rejected → voided
```

**Retry logic**:
- Si SUNAT timeout → reintentar 3 veces con backoff (5s, 15s, 45s)
- Si rechazado → NO reintentar, guardar error
- Si error de red → reintentar

### M2.6 — GRE API v2 Real (MEDIO)
**Dependencias**: M2.1
**Archivos a modificar**:
- `src/TukiFact.Infrastructure/Services/GreSunatClient.cs`

**GRE usa API REST (no SOAP)**:
```
# Auth
POST https://api-seguridad.sunat.gob.pe/v1/clientessol/{client_id}/oauth2/token

# Envio
POST https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/{filename}

# Consulta estado
GET https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/envios/{ticket}
```

**Cambios**:
- OAuth2 token management (access + refresh)
- Envio via REST multipart (no SOAP)
- Polling de ticket para estado async

---

## Criterios de Completado

- [ ] SunatClient envia a SUNAT beta Y produccion (configurable por tenant)
- [ ] Certificado .pfx se valida correctamente al upload
- [ ] Set de homologacion genera los ~22 documentos requeridos
- [ ] Cada documento de homologacion se puede enviar y verificar resultado
- [ ] Errores SUNAT se parsean y muestran correctamente en UI
- [ ] GRE se envia via API REST v2
- [ ] Retry logic para timeouts
- [ ] 0 errores lint, 0 warnings
- [ ] Build limpio frontend + backend

---

## Prerequisitos Externos

| Item | Necesitas | Como obtener |
|------|-----------|--------------|
| Certificado digital real | .pfx de empresa | SUNAT o proveedor certificado (Llama.pe, etc.) |
| SOL credentials | Usuario + clave SOL | Portal SUNAT |
| RUC activo | RUC de empresa habido | Ya tienes: 20613614509 |
| Client ID GRE | Para OAuth2 GRE | Portal developer SUNAT |
