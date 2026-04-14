# TukiFact vs Nubefact vs PSE/OSE — Comparativa Real

> Analisis basado en el codebase real de TukiFact al 2026-04-14
> vs funcionalidades publicas de Nubefact y PSE/OSE genericos

---

## Comprobantes Electronicos

| Feature | Nubefact | PSE/OSE | TukiFact | Estado |
|---------|----------|---------|----------|--------|
| Facturas (01) | SI | SI | SI | LISTO |
| Boletas (03) | SI | SI | SI | LISTO |
| Notas Credito (07) | SI | SI | SI | LISTO |
| Notas Debito (08) | SI | SI | SI | LISTO |
| Comunicacion Baja (RA) | SI | SI | SI | LISTO |
| Resumen Diario (RC) | SI | SI | SI | LISTO |
| Guias Remision (GRE T09) | SI | SI | SI | LISTO |
| Retenciones (20) | SI | SI | SI | LISTO |
| Percepciones (40) | SI | SI | SI | LISTO |

**Resultado: PARIDAD COMPLETA en comprobantes**

---

## Infraestructura Tecnica

| Feature | Nubefact | PSE/OSE | TukiFact | Estado |
|---------|----------|---------|----------|--------|
| PDF generacion | SI | SI | SI (QuestPDF) | LISTO |
| Firma digital X509 | SI | SI | SI (XMLDSig RSA-SHA256) | LISTO |
| UBL 2.1 compliant | SI | SI | SI | LISTO |
| Storage XML/CDR/PDF | Propio | Propio | SI (MinIO S3) | LISTO |
| Certificado upload UI | SI | SI | SI (.pfx) | LISTO |

---

## Ventajas TukiFact (lo que la competencia NO tiene)

| Feature | Nubefact | PSE/OSE | TukiFact | Tipo |
|---------|----------|---------|----------|------|
| Multi-tenant SaaS | NO (single) | NO (single) | SI (RLS + tenant resolver) | ARQUITECTURA |
| Cotizaciones → Factura | NO | NO | SI (convert-to-invoice) | FUNCIONAL |
| Facturacion recurrente | NO | NO | SI (BackgroundService + scheduling) | FUNCIONAL |
| Multi-moneda (USD/EUR) | Basico | Basico | SI (PaymentExchangeRate UBL) | FUNCIONAL |
| Catalogos SUNAT (600+ codes) | Parcial | Parcial | SI (seed completo) | DATOS |
| SIRE integracion (5 endpoints) | NO | NO | SI (OAuth2 REST) | INTEGRACION |
| Tipo cambio SBS automatico | NO | NO | SI (ExchangeRateService) | AUTOMATIZACION |
| Webhooks CRUD + deliveries + retries | Basico | NO | SI (full system) | DEVELOPER |
| IA asistente BYOK | NO | NO | SI (5 providers: Gemini, Claude, Grok, DeepSeek, OpenAI) | IA |
| Lookup RUC/DNI multi-provider | Propio (1) | NO | SI (4: ApiPeru, Migo, PeruAPI, APIs.net) | INTEGRACION |
| Backoffice SaaS completo | N/A | N/A | SI (9 endpoints + 8 paginas) | OPERACIONES |
| Detracciones SPOT | SI | SI | SI (UBL extension) | LISTO |
| Dark mode | NO | NO | SI (next-themes) | UX |
| Onboarding wizard | NO | Basico | SI (4 pasos con checks) | UX |
| RBAC (admin/emisor/consulta) | Basico | NO | SI (JWT claims) | SEGURIDAD |
| API Keys (tk_* + SHA256) | SI | NO | SI (generar + revocar) | DEVELOPER |

---

## Lo que FALTA para superar a todos

### Critico (sin esto no vas a produccion)

| Feature | Nubefact | TukiFact | Que falta |
|---------|----------|----------|-----------|
| SUNAT produccion real | SI | NO (stub mode) | SunatClient modo prod, URLs reales |
| Homologacion SUNAT | SI | NO | Set de ~25 documentos de prueba |
| GRE API v2 real | SI | NO | GreSunatClient contra REST real |
| Error handling SUNAT | SI | NO | Codigos rechazo, reenvio, estados |

### Alto (para ser competitivo como SaaS)

| Feature | Nubefact | TukiFact | Que falta |
|---------|----------|----------|-----------|
| Email auto PDF al cliente | SI | PARCIAL | Trigger automatico en NATS consumer |
| NATS consumers | N/A | PARCIAL | Workers que escuchen eventos |
| Notificaciones in-app | NO | NO | SSE endpoint + NotificationService |
| Rate limiting middleware | Propio | PARCIAL | Integrar InMemoryRateLimiter en pipeline |

### Medio (para operar SaaS)

| Feature | Nubefact | TukiFact | Que falta |
|---------|----------|----------|-----------|
| Impersonar tenant | N/A | NO | JWT temporal con tenant switch |
| Reportes MRR/Churn | N/A | NO | Metricas SaaS en backoffice |
| Gestion suscripciones | N/A | NO | Cobros, vencimientos, downgrade |
| CRUD empleados plataforma | N/A | NO | Solo lectura ahora |

### Developer Platform (TU diferencial de negocio)

| Feature | Nubefact | TukiFact | Que falta |
|---------|----------|----------|-----------|
| API documentada OpenAPI | SI (Swagger) | NO | OpenAPI 3.1 auto-generado |
| Docs interactiva | Swagger UI | NO | Scalar o Redoc |
| SDK Node.js | SI | NO | Auto-generar desde OpenAPI |
| SDK Python | NO | NO | Auto-generar desde OpenAPI |
| Sandbox/Playground | Parcial | NO | Tenant sandbox auto-creado |
| Developer Portal | NO | NO | /developers con todo integrado |
| Rate limit por plan | Propio | NO | Free=100/h, Empresa=ilimitado |

---

## Resumen Ejecutivo

```
COMPROBANTES:     TukiFact = Nubefact (paridad completa)
ARQUITECTURA:     TukiFact > Nubefact (multi-tenant, Clean Arch, .NET 10)
INTEGRACIONES:    TukiFact > Nubefact (SIRE, multi-provider lookup, IA BYOK)
FUNCIONALIDAD:    TukiFact > Nubefact (cotizaciones, recurrentes, multi-moneda)
DEVELOPER:        TukiFact < Nubefact (falta OpenAPI + SDK + docs)
PRODUCCION SUNAT: TukiFact < Nubefact (falta modo prod + homologacion)
```

**Conclusion**: TukiFact ya es MEJOR que Nubefact en features.
Lo unico que falta es: produccion SUNAT real + developer platform.
Esos son los Mega-Sprints 2 y 4.
