# 03 - Alcance del MVP

## Contexto del Equipo
- **Empresa**: Tukituki Solution SAC (RUC 20613614509)
- **Devs dedicados**: 2-3 personas
- **Conocimiento UBL 2.1**: Ninguno (arrancan de cero)
- **Stack elegido**: .NET 10 LTS, Python FastAPI, Next.js
- **Primer cliente**: Restaurante (muchas boletas) + Importador (boletas + facturas)
- **Sin deadline**: Calidad sobre velocidad

## LO QUE ENTRA EN EL MVP

### Módulo 1: Core de Emisión
- Factura Electrónica (tipo 01) - UBL 2.1
- Boleta de Venta Electrónica (tipo 03) - UBL 2.1
- Nota de Crédito Electrónica (tipo 07) - 10 motivos (Cat. 09)
- Nota de Débito Electrónica (tipo 08) - 3 motivos (Cat. 10)
- Resumen Diario de Boletas (MANDATORIO)
- Comunicación de Baja
- Generación de XML UBL 2.1 completo
- Firma Digital X.509 (certificado de pruebas para beta)
- Generación de PDF (representación impresa)
- Almacenamiento de XML + PDF + CDR

### Módulo 2: Motor Tributario (Básico)
- IGV: Gravado (10), Exonerado (20), Inafecto (30), Exportación (40), Gratuito (11-16, 21, 31-36)
- Tipo de cambio SUNAT automático (facturación en USD)
- Cargos y descuentos (Cat. 53) - nivel global e ítem
- ICBPER (bolsas plásticas) - obligatorio y simple
- Catálogos SUNAT core: 01, 02, 03, 05, 06, 07, 09, 10, 13 (UBIGEO básico), 15, 51

### Módulo 3: Gateway SUNAT
- Envío SOAP a webservice de SUNAT
- Recepción y procesamiento de CDR
- Cola de reintentos para envíos fallidos (NATS JetStream)
- Consulta de estado de documentos
- Manejo de errores SUNAT (códigos de rechazo/observación)
- Modo sandbox (entorno beta SUNAT)

### Módulo 4: Multi-Tenant
- Registro de empresas (tenant) con RUC, razón social, datos
- Gestión de series y correlativos por empresa y punto de emisión
- Gestión de certificados digitales por empresa (vault básico)
- Aislamiento de datos por tenant (Row Level Security PostgreSQL)

### Módulo 5: API REST
- Endpoints CRUD para cada tipo de documento
- Documentación OpenAPI/Swagger auto-generada
- API Keys por empresa
- Rate limiting por plan
- Webhooks para notificación de CDR (aceptado/rechazado/observado)
- SDK de ejemplo (Python y JavaScript)

### Módulo 6: Panel Web (Frontend)
- Login / Registro multiempresa
- Dashboard con KPIs: docs emitidos, aceptados, rechazados, pendientes
- Emisión manual de factura/boleta/notas desde el panel
- Consulta y búsqueda de documentos emitidos
- Descarga de XML + PDF + CDR
- Configuración de empresa (datos, logo, series, certificado)
- Gestión de API Keys

### Módulo 7: IA - Agente Emisor (Thin Layer)
- UN solo agente: Agente Emisor
- Funcionalidades:
  - "¿Por qué me rechazaron esta factura?" → analiza CDR
  - "Ayudame a emitir una nota de crédito" → guía paso a paso
  - "¿Cuántas boletas tengo pendientes de resumen?" → consulta
  - "¿Qué serie debo usar?" → explica reglas
- BYOK básico: OpenAI y Anthropic (2 proveedores)
- Chat embebido en panel web (WebSocket via FastAPI)
- Knowledge base: reglas SUNAT + catálogos + errores comunes

### Módulo 8: Infraestructura
- Docker Compose completo (dev + staging)
- PostgreSQL 18 con RLS para multi-tenant
- MinIO para almacenamiento de documentos (XML, PDF, CDR)
- NATS JetStream para colas de emisión y reintentos
- Logs estructurados
- Health checks por servicio
- CI básico (GitHub Actions)

## LO QUE NO ENTRA EN EL MVP

| Feature | Fase | Razón |
|---------|:----:|-------|
| GRE (Remitente, Transportista, Eventos) | 2 | Complejidad alta, requiere UBIGEO completo |
| Detracciones (SPOT) | 2 | 30+ códigos, reglas complejas |
| Retenciones / Percepciones | 2 | Requiere ser agente designado |
| ISC (3 sistemas de cálculo) | 2 | Nicho |
| IVAP | 3 | Muy específico (arroz pilado) |
| Integración SIRE | 3 | Requiere emisión funcionando primero |
| Comprobante de Retención/Percepción | 3 | Documento especial |
| Liquidación de Compra | 3 | Nicho |
| White-label completo | 4 | Requiere madurez del producto |
| 6 Agentes IA completos | 3-4 | Necesitan operaciones reales |
| Factura negociable (factoring) | 5 | Requiere integración fintech |
| OpenTelemetry avanzado | 2-3 | Logs básicos primero |
| Kubernetes | Cuando escale | Docker Compose alcanza |
| App móvil | 4+ | Web responsive primero |
