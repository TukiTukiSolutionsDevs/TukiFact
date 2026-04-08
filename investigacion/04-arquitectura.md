# 04 - Arquitectura Técnica y Servicios

## Diagrama General

```
                        ┌─────────────────────┐
                        │     Next.js 16       │
                        │     (Frontend)       │
                        │  SSR + Auth Middleware│
                        │  Cookie Sessions     │
                        └──────────┬──────────┘
                                   │ HTTPS / REST
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                  ASP.NET Core (.NET 10 LTS)                     │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │              MIDDLEWARE PIPELINE                            │  │
│  │  TenantResolver → JWT Auth → RateLimit → CORS → Router    │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │
│  │  Emission     │ │  Tax Engine  │ │  SUNAT Gateway           │ │
│  │  Service      │ │              │ │  (SOAP client,           │ │
│  │  (XML gen,    │ │  (IGV calc,  │ │   CDR processing,        │ │
│  │   validation, │ │   ICBPER,    │ │   retry queue,           │ │
│  │   signing)    │ │   TC SUNAT)  │ │   contingency)           │ │
│  └──────────────┘ └──────────────┘ └──────────────────────────┘ │
│                                                                  │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │
│  │  Tenant      │ │  Catalog     │ │  Series                  │ │
│  │  Manager     │ │  Service     │ │  Manager                 │ │
│  │  (empresas,  │ │  (58+ cats,  │ │  (numeración atómica,    │ │
│  │   config,    │ │   versioning)│ │   puntos de emisión)     │ │
│  │   certs)     │ │              │ │                          │ │
│  └──────────────┘ └──────────────┘ └──────────────────────────┘ │
│                                                                  │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │
│  │  PDF         │ │  Webhook     │ │  Auth                    │ │
│  │  Generator   │ │  Service     │ │  Service                 │ │
│  │  (represent. │ │  (notificar  │ │  (JWT, API Keys,         │ │
│  │   impresa)   │ │   CDR a      │ │   RBAC, registro)        │ │
│  │              │ │   clientes)  │ │                          │ │
│  └──────────────┘ └──────────────┘ └──────────────────────────┘ │
└──────────┬──────────────────┬──────────────────┬────────────────┘
           │                  │                  │
     NATS JetStream     EF Core + RLS       HTTP interno
           │                  │                  │
           ▼                  ▼                  ▼
    ┌────────────┐    ┌──────────────┐    ┌──────────────┐
    │   NATS     │    │ PostgreSQL   │    │   FastAPI     │
    │ JetStream  │    │     18       │    │   (Python)    │
    │            │    │              │    │               │
    │ Streams:   │    │ Schemas:     │    │ - AI Agent    │
    │ - emission │    │ - public     │    │ - WebSocket   │
    │ - cdr      │    │ - catalog    │    │ - BYOK router │
    │ - webhook  │    │              │    │ - RAG engine  │
    │ - retry    │    │ RLS policies │    │               │
    │            │    │ per tenant   │    │ Knowledge:    │
    │ Queue      │    │              │    │ - Reglas SUNAT│
    │ Groups:    │    │ Tables:      │    │ - Catálogos   │
    │ - workers  │    │ ~25 tablas   │    │ - Errores CDR │
    └────────────┘    └──────────────┘    └──────────────┘
                              │
                              │
                      ┌──────────────┐
                      │    MinIO     │
                      │  (S3-compat) │
                      │              │
                      │ Buckets:     │
                      │ - xml/       │
                      │ - pdf/       │
                      │ - cdr/       │
                      │ - certs/     │
                      └──────────────┘
```

## Servicios Internos (.NET 10)

### 1. Emission Service
- **Responsabilidad**: Generar XML UBL 2.1, validar contra reglas SUNAT, firmar digitalmente
- **Input**: JSON con datos del documento
- **Output**: XML firmado listo para envío
- **Dependencias**: Tax Engine, Catalog Service, Series Manager, Cert Vault

### 2. Tax Engine
- **Responsabilidad**: Calcular IGV, aplicar afectaciones, tipo de cambio, ICBPER
- **Input**: Líneas de detalle con precios y cantidades
- **Output**: Montos calculados con tributos desglosados
- **Dependencias**: Catalog Service (Cat. 05, 07)

### 3. SUNAT Gateway
- **Responsabilidad**: Enviar XML a SUNAT via SOAP, recibir CDR, manejar reintentos
- **Input**: XML firmado
- **Output**: CDR procesado con estado (aceptado/rechazado/observado)
- **Dependencias**: NATS (cola de emisión y reintentos)
- **Endpoints SUNAT**:
  - Producción: `https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService`
  - Beta: `https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService`

### 4. Tenant Manager
- **Responsabilidad**: CRUD de empresas, configuración, gestión de certificados
- **Input**: Datos de empresa (RUC, razón social, dirección, cert)
- **Output**: Tenant configurado con series y certificado

### 5. Catalog Service
- **Responsabilidad**: Servir catálogos SUNAT, versionamiento, actualización
- **Datos**: 58+ catálogos precargados, actualizables
- **Cache**: En memoria con invalidación por versión

### 6. Series Manager
- **Responsabilidad**: Generación atómica de series y correlativos
- **Reglas**: F para facturas, B para boletas, F/B para notas según origen
- **Concurrencia**: Lock optimista con incremento atómico en PostgreSQL

### 7. PDF Generator
- **Responsabilidad**: Generar representación impresa del documento
- **Templates**: Configurables por tenant (logo, colores)
- **Formato**: A4, Ticket

### 8. Webhook Service
- **Responsabilidad**: Notificar a clientes cuando un CDR llega
- **Mecanismo**: HTTP POST al endpoint configurado por el tenant
- **Reintentos**: 3 intentos con backoff exponencial via NATS

### 9. Auth Service
- **Responsabilidad**: Registro, login, JWT, API Keys, RBAC
- **Tokens**: JWT con tenant_id en claims
- **API Keys**: Para integraciones M2M (machine-to-machine)
- **Roles**: admin, emisor, consulta

## Flujo de Emisión Completo

```
1. Cliente envía POST /api/v1/invoices con JSON
2. Middleware: TenantResolver extrae tenant_id del JWT/API Key
3. Middleware: Auth valida permisos
4. Middleware: RateLimit verifica quota del plan
5. Controller recibe request validado
6. Tax Engine calcula tributos
7. Series Manager asigna número correlativo (atómico)
8. Emission Service genera XML UBL 2.1
9. Emission Service firma con certificado X.509 del tenant
10. Se publica mensaje en NATS stream "emission"
11. Worker (Queue Group) consume el mensaje:
    a. SUNAT Gateway envía SOAP a SUNAT
    b. Recibe CDR
    c. Procesa respuesta (aceptado/rechazado/observado)
    d. Almacena XML + CDR en MinIO
    e. Genera PDF y almacena en MinIO
    f. Actualiza estado en PostgreSQL
    g. Publica en NATS stream "webhook"
12. Webhook worker notifica al cliente (si tiene webhook configurado)
13. API responde al cliente con el estado del documento
```

## Comunicación entre Servicios

| Desde | Hacia | Protocolo | Patrón |
|-------|-------|-----------|--------|
| Frontend → API | ASP.NET Core | HTTPS/REST | Request-Reply |
| API → SUNAT | SUNAT WS | SOAP/HTTPS | Request-Reply (async via NATS) |
| API → NATS | NATS | TCP | Publish (JetStream) |
| Workers → NATS | NATS | TCP | Subscribe (Queue Group) |
| API → PostgreSQL | PostgreSQL | TCP | EF Core + RLS |
| API → MinIO | MinIO | HTTP/S3 | PUT/GET objetos |
| API → FastAPI (AI) | FastAPI | HTTP/WebSocket | Request-Reply + Streaming |
| FastAPI → PostgreSQL | PostgreSQL | TCP | SQLAlchemy (read-only) |
