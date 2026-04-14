# 05 — SIRE (Sistema Integrado de Registros Electrónicos)

> **Prioridad**: 🟢 DIFERENCIADOR — Nubefact básico NO lo integra.
> **Normativa**: R.S. 112-2021, R.S. 040-2022, R.S. 293-2024, R.S. 217-2025, R.S. 392-2025
> **Fuente**: sire.sunat.gob.pe, cpe.sunat.gob.pe

---

## ¿Qué es?

El SIRE reemplaza al PLE (Programa de Libros Electrónicos). Gestiona el Registro de Ventas e Ingresos (RVIE) y el Registro de Compras (RCE) de forma electrónica.

## Obligatoriedad (Actualizado 2026)

| Grupo | Obligatorio desde |
|-------|:----------------:|
| RER + MYPE Tributario | Octubre 2023 |
| Régimen General (no PRICOS) | Enero 2025 |
| Principales Contribuyentes (PRICOS) con ingresos > 2300 UIT | **Junio 2026** (R.S. 392-2025) |
| Demás PRICOS | Enero 2026 |
| Nuevos RUC desde agosto 2024 | Desde inicio actividades |

## Arquitectura API REST

### Autenticación (mismo OAuth2 que GRE)

```
POST https://api-seguridad.sunat.gob.pe/v1/clientessol/{client_id}/oauth2/token/
Body: grant_type=password, scope, client_id, client_secret, username={RUC}{SOL_USER}, password={SOL_PASS}
```

### Flujo Principal RVIE

```
1. Generar token OAuth2
2. Consultar año/mes disponible
3. Descargar propuesta SUNAT (SUNAT pre-genera con tus CPE emitidos)
4. Aceptar propuesta (si no hay cambios)
   — o —
   Importar reemplazo (si hay correcciones)
5. Generar registro (obtener ticket)
6. Consultar estado del ticket
7. Descargar reporte generado
```

### Servicios API Principales

| Servicio | Método | Descripción |
|----------|--------|-------------|
| Api Seguridad | POST | Generar token |
| Consultar año/mes | GET | Periodos disponibles |
| Aceptar propuesta | POST | Aceptar RVIE propuesto |
| Importar reemplazo | POST (TUS) | Reemplazar propuesta completa |
| Importar nuevos CDP | POST (TUS) | Agregar comprobantes faltantes |
| Importar ajustes | POST (TUS) | Rectificar periodos anteriores |
| Consultar ticket | GET | Estado del envío |
| Descargar reporte | GET | PDF/Excel del registro |
| Descargar inconsistencias | GET | Errores encontrados |

**IMPORTANTE**: Los servicios TUS (Tus Upload Standard) para carga masiva originalmente requieren JAVA según SUNAT, pero se puede implementar con HTTP estándar multipart desde .NET.

**IMPORTANTE**: NO consumir desde cliente web — CORS bloqueado por SUNAT. Siempre desde backend.

## Implementación en TukiFact

### Valor diferenciador

La mayoría de plataformas como Nubefact solo emiten CPE. NO gestionan el SIRE. TukiFact puede:
1. Generar el RVIE automáticamente desde los documentos emitidos
2. Aceptar/rechazar la propuesta SUNAT
3. Importar ajustes
4. Todo desde el panel web — sin entrar a SUNAT SOL

### Archivos a crear

| Archivo | Descripción |
|---------|-------------|
| `Infrastructure/Services/SireClient.cs` | REST client para API SIRE |
| `API/Controllers/SireController.cs` | Endpoints gestión SIRE |
| Frontend `app/(authenticated)/sire/page.tsx` | Página SIRE |
| `Infrastructure/Services/SireReportGenerator.cs` | Genera archivo RVIE desde documentos |

### Estructura archivo RVIE

Formato definido por Anexo N° 1 de R.S. 112-2021:
- Pipe-delimited (`|`)
- Campos: periodo, CUO, correlativo, fecha emisión, fecha vencimiento, tipo CDP, serie, número, tipo doc cliente, número doc cliente, razón social, base imponible, IGV, total, tipo cambio, etc.
