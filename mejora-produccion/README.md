# TukiFact — Plan de Mejora para Producción

> **Objetivo**: Completar TODAS las funcionalidades necesarias para que TukiFact sea competitivo (y superior) a Nubefact, PSE y otras plataformas de facturación electrónica peruanas.
>
> **Fecha**: 2026-04-13
> **Investigado con**: Tavily Research + documentación oficial SUNAT + cpe.sunat.gob.pe

---

## Estado Actual

TukiFact tiene un core sólido:
- ✅ 18 controllers, 17 entities, 27+ endpoints
- ✅ Factura (01), Boleta (03), NC (07), ND (08)
- ✅ Resumen Diario + Comunicación de Baja
- ✅ XML UBL 2.1 + Firma X.509 + PDF (QuestPDF)
- ✅ Multi-tenant con RLS PostgreSQL
- ✅ Auth JWT + RBAC + API Keys
- ✅ Backoffice SaaS (9 endpoints)
- ✅ Catálogo Productos + Directorio Clientes
- ✅ Docker prod stack (7 servicios)
- ✅ SUNAT Beta (stub)

## Lo Que Falta

Ver `ROADMAP.md` para el plan detallado con 4 batches y cada documento individual para especificaciones técnicas.

## Estructura de Documentos

```
mejora-produccion/
├── README.md                          ← Este archivo
├── ROADMAP.md                         ← Roadmap detallado con batches y estimaciones
├── 01-gre-guia-remision.md            ← Guía de Remisión Electrónica (GRE-R y GRE-T)
├── 02-sunat-produccion.md             ← Endpoints SUNAT producción (SOAP + REST)
├── 03-detracciones-spot.md            ← Sistema de Detracciones (Cat. 54)
├── 04-icbper.md                       ← Impuesto bolsas plásticas
├── 05-sire-integracion.md             ← SIRE (Registro de Ventas/Compras)
├── 06-tipo-cambio-ruc.md              ← Tipo de cambio SUNAT + Validar RUC
├── 07-retenciones-percepciones.md     ← Comprobantes de Retención/Percepción
├── 08-email-notificaciones.md         ← Email transaccional + notificaciones
└── 09-funcionalidades-complementarias.md ← Rate limiting, forgot password, catálogos, multi-moneda
```

## Stack Tecnológico Actual

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core (.NET 10) |
| Frontend | Next.js 16 + Tailwind v4 + shadcn/ui |
| Database | PostgreSQL 18 + RLS |
| Messaging | NATS JetStream |
| Storage | MinIO (S3-compatible) |
| Containers | Docker Compose (7 servicios) |
| XML | UBL 2.1 SUNAT |
| Firma | XMLDSig + X509 RSA-SHA256 |
| Reverse Proxy | Nginx |

## Cómo Usar Este Plan

1. Leer `ROADMAP.md` para entender el orden de ejecución
2. Cada batch tiene sus documentos de especificación técnica
3. Empezar siempre por el Batch A (bloqueante para producción)
4. Cada documento tiene: contexto, especificación técnica, endpoints, archivos a crear/modificar
