# 01 - Análisis de Propuesta y Huecos Encontrados

## Empresa
- **Razón Social**: Tukituki Solution SAC
- **RUC**: 20613614509
- **Giro**: Consultora de software
- **Estado actual**: Emite desde portal SOL de SUNAT + conexiones Java directas por cada aplicativo

## Problema Central
Cada aplicativo vendido (restaurantes, e-commerce, cafeterías, importadoras, ERP RRHH) tiene su propia conexión Java a SUNAT. No hay centralización, no hay control, no hay trazabilidad. Si un cliente tiene un problema tributario, no se puede ayudar porque los datos están dispersos.

## Visión del Producto
Crear **TukiFact**: una plataforma SaaS de facturación electrónica peruana que funcione como núcleo formal de todos los sistemas. Con orquestación de agentes IA especializados, modelo BYOK (Bring Your Own Key), arquitectura Docker/microservicios, y potencial white-label.

## Lo Que Está Bien de la Propuesta Original
1. Visión de plataforma (infraestructura reusable), no de producto aislado
2. Docker como filosofía base — microservicios contenerizados
3. Multiempresa, multirubro, multiaplicación — modelo SaaS serio
4. BYOK — no atarse a un solo proveedor de IA
5. Visión PSE/OSE a futuro sin forzarlo ahora

## Huecos Críticos Encontrados

### 1. Documentos Electrónicos Faltantes
| Documento | Mencionado | Criticidad |
|-----------|:---:|:---:|
| GRE Transportista | NO | CRÍTICA |
| GRE por Eventos (R.S. 304-2024) | NO | ALTA |
| Resumen Diario de Boletas | NO | CRÍTICA (mandatorio) |
| Comprobante de Retención Electrónica | NO | ALTA |
| Comprobante de Percepción Electrónica | NO | ALTA |
| Liquidación de Compra Electrónica | NO | ALTA |
| Recibo por Honorarios Electrónico | NO | MEDIA |

**El Resumen Diario es MANDATORIO.** Las boletas NO se envían individualmente a SUNAT — se envían en resúmenes diarios.

### 2. Motor Tributario Subestimado
- IGV tiene 17 tipos de afectación (Catálogo 07), no solo "18%"
- ISC tiene 3 sistemas distintos de cálculo
- Detracciones (SPOT): 30+ códigos en Catálogo 54
- Retenciones: 3% del IGV
- Percepciones: 0.5%, 1%, 2% según caso
- IVAP: régimen especial arroz pilado
- ICBPER: impuesto bolsas plásticas
- IGV cambia composición interna 2026-2029 (IPM sube, IGV baja, total sigue 18%)

### 3. SIRE No Mencionado
El Sistema Integrado de Registros Electrónicos es el futuro de la contabilidad tributaria peruana. Ya es obligatorio desde enero 2025 para contribuyentes no principales. SUNAT propone automáticamente el Registro de Compras. Un sistema superior a NubeFact DEBE integrar SIRE.

### 4. Gestión de Contingencia Ausente
- Comprobantes de contingencia (SUNAT caído)
- Cola de reintentos para envíos fallidos
- CDR pendientes
- Modo offline para puntos de venta sin conexión
- Timeout handling del webservice SUNAT

### 5. Certificados Digitales No Detallados
- Vault de certificados (no texto plano)
- Alertas de vencimiento
- Rotación segura sin downtime
- Soporte para múltiples proveedores (SUNAT, Llama.pe, CertiSur)

### 6. Catálogos SUNAT (58+)
SUNAT tiene 58+ catálogos de códigos que necesitan versionamiento y actualización constante. Esto es INFRAESTRUCTURA, no un detalle menor.

### 7. Serie y Correlativo
Cada punto de emisión necesita serie única. En multiempresa se multiplica exponencialmente. Se necesita servicio de numeración atómica.

## Decisión
Todos estos huecos se resuelven por FASES. El MVP cubre lo esencial (factura, boleta, notas, resumen diario, comunicación de baja). Lo demás entra en el plan de mejoras post-MVP.
