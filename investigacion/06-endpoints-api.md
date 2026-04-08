# 06 - Endpoints de API REST

## Base URL
- **Producción**: `https://api.tukifact.com/v1`
- **Sandbox**: `https://sandbox.tukifact.com/v1`

## Autenticación
- **Header**: `Authorization: Bearer <JWT>` o `X-Api-Key: <API_KEY>`
- **Tenant**: Se extrae del JWT claim `tenant_id` o del API Key

## Rate Limiting
- Header de respuesta: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`
- Límites por plan (ver 08-modelo-pricing.md)

---

## Endpoints de Documentos

### Facturas
```
POST   /v1/invoices              Emitir factura
GET    /v1/invoices              Listar facturas (paginado + filtros)
GET    /v1/invoices/{id}         Obtener factura por ID
GET    /v1/invoices/{id}/xml     Descargar XML
GET    /v1/invoices/{id}/pdf     Descargar PDF
GET    /v1/invoices/{id}/cdr     Descargar CDR
GET    /v1/invoices/{id}/status  Consultar estado SUNAT
```

### Boletas
```
POST   /v1/receipts              Emitir boleta
GET    /v1/receipts              Listar boletas (paginado + filtros)
GET    /v1/receipts/{id}         Obtener boleta por ID
GET    /v1/receipts/{id}/xml     Descargar XML
GET    /v1/receipts/{id}/pdf     Descargar PDF
```

### Notas de Crédito
```
POST   /v1/credit-notes          Emitir nota de crédito
GET    /v1/credit-notes          Listar notas de crédito
GET    /v1/credit-notes/{id}     Obtener nota por ID
GET    /v1/credit-notes/{id}/xml Descargar XML
GET    /v1/credit-notes/{id}/pdf Descargar PDF
GET    /v1/credit-notes/{id}/cdr Descargar CDR
```

### Notas de Débito
```
POST   /v1/debit-notes           Emitir nota de débito
GET    /v1/debit-notes           Listar notas de débito
GET    /v1/debit-notes/{id}      Obtener nota por ID
GET    /v1/debit-notes/{id}/xml  Descargar XML
GET    /v1/debit-notes/{id}/pdf  Descargar PDF
GET    /v1/debit-notes/{id}/cdr  Descargar CDR
```

### Resúmenes Diarios
```
POST   /v1/daily-summaries              Generar resumen diario
GET    /v1/daily-summaries              Listar resúmenes
GET    /v1/daily-summaries/{id}         Obtener resumen
GET    /v1/daily-summaries/{id}/status  Consultar estado
```

### Comunicaciones de Baja
```
POST   /v1/void-communications          Solicitar baja de documentos
GET    /v1/void-communications          Listar comunicaciones
GET    /v1/void-communications/{id}     Obtener comunicación
GET    /v1/void-communications/{id}/status  Consultar estado
```

---

## Endpoints de Configuración

### Empresa (Tenant)
```
GET    /v1/company                Obtener datos de la empresa
PUT    /v1/company                Actualizar datos de la empresa
POST   /v1/company/certificate    Subir certificado digital
GET    /v1/company/certificate    Info del certificado (sin datos sensibles)
```

### Series
```
GET    /v1/series                 Listar series activas
POST   /v1/series                 Crear nueva serie
PUT    /v1/series/{id}            Actualizar serie
DELETE /v1/series/{id}            Desactivar serie
```

### API Keys
```
GET    /v1/api-keys               Listar API keys
POST   /v1/api-keys               Crear nueva API key
DELETE /v1/api-keys/{id}          Revocar API key
```

### Webhooks
```
GET    /v1/webhooks               Listar webhooks configurados
POST   /v1/webhooks               Crear webhook
PUT    /v1/webhooks/{id}          Actualizar webhook
DELETE /v1/webhooks/{id}          Eliminar webhook
POST   /v1/webhooks/{id}/test     Enviar webhook de prueba
```

---

## Endpoints de Consulta

### Catálogos SUNAT
```
GET    /v1/catalogs                        Listar catálogos disponibles
GET    /v1/catalogs/{number}               Obtener catálogo por número
GET    /v1/catalogs/{number}/codes          Listar códigos del catálogo
GET    /v1/catalogs/{number}/codes/{code}   Obtener código específico
```

### Utilidades
```
GET    /v1/utils/exchange-rate             Tipo de cambio SUNAT del día
GET    /v1/utils/validate-ruc/{ruc}        Validar RUC en padrón SUNAT
GET    /v1/utils/ubigeo                    Listar UBIGEO
```

### Dashboard / Métricas
```
GET    /v1/dashboard/summary               KPIs del dashboard
GET    /v1/dashboard/documents-by-status    Documentos por estado
GET    /v1/dashboard/documents-by-type      Documentos por tipo
GET    /v1/dashboard/monthly-totals         Totales mensuales
GET    /v1/usage                            Uso actual del plan
```

---

## Endpoints de Auth
```
POST   /v1/auth/register           Registrar nueva empresa + usuario admin
POST   /v1/auth/login              Login (devuelve JWT)
POST   /v1/auth/refresh            Refrescar JWT
POST   /v1/auth/forgot-password    Solicitar reset de password
POST   /v1/auth/reset-password     Resetear password
GET    /v1/auth/me                 Obtener usuario actual
```

### Usuarios (admin only)
```
GET    /v1/users                   Listar usuarios del tenant
POST   /v1/users                   Crear usuario
PUT    /v1/users/{id}              Actualizar usuario
DELETE /v1/users/{id}              Desactivar usuario
```

---

## Endpoints de IA
```
WS     /v1/ai/chat                WebSocket para chat con agente IA
POST   /v1/ai/ask                 Pregunta simple (request-reply)
GET    /v1/ai/providers            Listar proveedores IA configurados
PUT    /v1/ai/providers/{provider} Configurar API key de proveedor IA
```

---

## Ejemplo de Request/Response

### POST /v1/invoices
```json
{
  "operation_type": "0101",
  "currency": "PEN",
  "issue_date": "2026-04-07",
  "due_date": "2026-05-07",
  "customer": {
    "doc_type": "6",
    "doc_number": "20123456789",
    "name": "Empresa Cliente SAC",
    "address": "Av. Ejemplo 123, Lima"
  },
  "items": [
    {
      "description": "Servicio de consultoría",
      "quantity": 1,
      "unit_code": "ZZ",
      "unit_price": 1000.00,
      "igv_type": "10"
    }
  ],
  "legends": [
    { "code": "1000", "value": "MIL CIENTO OCHENTA Y 00/100 SOLES" }
  ],
  "external_id": "order-12345"
}
```

### Response 201 Created
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "document_type": "01",
  "full_number": "F001-00000001",
  "sunat_status": "accepted",
  "sunat_response_code": "0",
  "total_gravado": 1000.00,
  "total_igv": 180.00,
  "total_venta": 1180.00,
  "xml_url": "/v1/invoices/550e8400.../xml",
  "pdf_url": "/v1/invoices/550e8400.../pdf",
  "cdr_url": "/v1/invoices/550e8400.../cdr",
  "created_at": "2026-04-07T15:30:00Z"
}
```
