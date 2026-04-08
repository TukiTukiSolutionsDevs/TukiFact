# 02 - Ecosistema Documental SUNAT Completo

## Marco Regulatorio Base
- **R.S. 300-2014/SUNAT**: Sistema de Emisión Electrónica (SEE) general
- **R.S. 097-2012/SUNAT**: SEE desde sistemas del contribuyente
- **R.S. 199-2015/SUNAT**: Proveedor de Servicios Electrónicos (PSE)
- **R.S. 117-2017/SUNAT**: Operador de Servicios Electrónicos (OSE)
- **R.S. 123-2022/SUNAT**: Guía de Remisión Electrónica (GRE) nueva estructura XML
- **R.S. 304-2024/SUNAT**: GRE nuevo modelo
- **R.S. 240-2024/SUNAT**: GRE comercio exterior

## Estándar Técnico
- **UBL 2.1** (Universal Business Language) — obligatorio
- **XML** con firma digital X.509 (clave privada 2048 bits)
- **CDR** (Constancia de Recepción) como respuesta de SUNAT
- **SOAP** webservice para envío a SUNAT (NO REST)

## Tipos de Documentos Electrónicos

### Comprobantes de Pago
| Código | Documento | Serie | Envío |
|:------:|-----------|-------|-------|
| 01 | Factura Electrónica | FXXX | Individual a SUNAT |
| 03 | Boleta de Venta Electrónica | BXXX | Resumen Diario |
| 07 | Nota de Crédito Electrónica | F/BXXX (hereda) | Individual |
| 08 | Nota de Débito Electrónica | F/BXXX (hereda) | Individual |

### Documentos Vinculados
| Documento | Serie | Envío |
|-----------|-------|-------|
| Guía Remisión Remitente (GRE-R) | TXXX (sistema) / EGXX (SOL) | Individual |
| Guía Remisión Transportista (GRE-T) | VXXX | Individual |
| Guía Remisión por Eventos | — | Complementa GRE existente |
| Comprobante Retención | R001 | Individual |
| Comprobante Percepción | P001 | Individual |
| Liquidación de Compra | — | Individual |

### Documentos de Resumen/Baja
| Documento | Uso |
|-----------|-----|
| Resumen Diario | Enviar boletas acumuladas del día |
| Comunicación de Baja | Anular facturas y notas |

## Catálogos SUNAT Principales

### Cat. 01 - Tipo de Documento
| Código | Descripción |
|:------:|-------------|
| 01 | Factura |
| 03 | Boleta de Venta |
| 07 | Nota de Crédito |
| 08 | Nota de Débito |
| 09 | Guía de Remisión Remitente |
| 20 | Comprobante de Retención |
| 40 | Comprobante de Percepción |
| 31 | Guía de Remisión Transportista |

### Cat. 06 - Tipo de Documento de Identidad
| Código | Descripción |
|:------:|-------------|
| 0 | DOC.TRIB.NO.DOM.SIN.RUC |
| 1 | DNI |
| 4 | Carnet de Extranjería |
| 6 | RUC |
| 7 | Pasaporte |
| A | Cédula Diplomática |

### Cat. 07 - Tipo de Afectación del IGV
| Código | Descripción |
|:------:|-------------|
| 10 | Gravado - Operación Onerosa |
| 11 | Gravado - Retiro por premio |
| 12 | Gravado - Retiro por donación |
| 13 | Gravado - Retiro |
| 14 | Gravado - Retiro por publicidad |
| 15 | Gravado - Bonificaciones |
| 16 | Gravado - Retiro por entrega a trabajadores |
| 17 | Gravado - IVAP |
| 20 | Exonerado - Operación Onerosa |
| 21 | Exonerado - Transferencia Gratuita |
| 30 | Inafecto - Operación Onerosa |
| 31 | Inafecto - Retiro por Bonificación |
| 32 | Inafecto - Retiro |
| 33 | Inafecto - Retiro por Muestras Médicas |
| 34 | Inafecto - Retiro por Convenio Colectivo |
| 35 | Inafecto - Retiro por premio |
| 36 | Inafecto - Retiro por publicidad |
| 40 | Exportación de bienes o servicios |

### Cat. 05 - Códigos de Tributos
| Código | Nombre | Internacional |
|:------:|--------|:------------:|
| 1000 | IGV | VAT |
| 2000 | ISC | EXC |
| 3000 | IR (Renta) | TOX |
| 7152 | ICBPER | OTH |
| 9995 | Exportación | FRE |
| 9996 | Gratuito | FRE |
| 9997 | Exonerado | VAT |
| 9998 | Inafecto | FRE |
| 9999 | Otros tributos | OTH |

### Cat. 09 - Tipo de Nota de Crédito (motivos)
| Código | Descripción |
|:------:|-------------|
| 01 | Anulación de la operación |
| 02 | Anulación por error en el RUC |
| 03 | Corrección por error en la descripción |
| 04 | Descuento global |
| 05 | Descuento por ítem |
| 06 | Devolución total |
| 07 | Devolución por ítem |
| 08 | Bonificación |
| 09 | Disminución en el valor |
| 10 | Otros conceptos |

### Cat. 10 - Tipo de Nota de Débito (motivos)
| Código | Descripción |
|:------:|-------------|
| 01 | Intereses por mora |
| 02 | Aumento en el valor |
| 03 | Penalidades / otros conceptos |

### Cat. 51 - Tipo de Operación (UBL 2.1)
| Código | Descripción | Comprobante |
|:------:|-------------|-------------|
| 0101 | Venta interna | Factura, Boleta |
| 0112 | Venta Interna - Gasto Deducible PN | Factura |
| 0113 | Venta Interna - NRUS | Boleta |
| 0200 | Exportación | Factura |
| 0401 | Venta Interna - Anticipos | Factura |

## IGV - Estructura Actual y Cambios 2026-2029
| Año | IPM | IGV | Total |
|:---:|:---:|:---:|:-----:|
| 2026 | 2.5% | 15.5% | 18% |
| 2027 | 3.0% | 15.0% | 18% |
| 2028 | 3.5% | 14.5% | 18% |
| 2029 | 4.0% | 14.0% | 18% |

La tasa total sigue siendo 18%, pero la composición interna cambia. El motor tributario debe manejar esto para reportes y desglose XML.

## Mecanismos de Recaudación Adelantada
| Mecanismo | Porcentaje | Catálogo |
|-----------|-----------|----------|
| Detracciones (SPOT) | 4% a 12% según bien/servicio | Cat. 54 (30+ códigos) |
| Retenciones | 3% del IGV | — |
| Percepciones | 0.5%, 1%, 2% | Cat. 22 |

## Flujo de Emisión de Factura
```
Datos → Validación → Reglas tributarias → XML UBL 2.1 → Firma X.509
→ Envío SOAP a SUNAT → CDR (Aceptada/Rechazada/Observada)
→ Almacenar XML+PDF+CDR → Notificar
```

## Flujo de Emisión de Boleta
```
Boletas del día → Acumular → Generar Resumen Diario (XML)
→ Firma → Envío SOAP → Ticket → Consultar estado → CDR del resumen
```

## Flujo de Comunicación de Baja
```
Solicitud → XML especial → Envío SOAP → Ticket (asíncrono)
→ Consultar estado → CDR de la baja
```

## Requisitos PSE/OSE (futuro)
- **PSE**: Capital >= 150 UIT, 5+ trabajadores, ISO 27001, R.S. 199-2015
- **OSE**: Capital >= 300 UIT, carta fianza, ISO 27001, R.S. 117-2017, certificado 2048-bit
- Ambos: inscripción en registro SUNAT, homologación, supervisión periódica

## Entorno de Pruebas SUNAT
- **URL Beta**: `https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService`
- Requiere clave SOL para acceso
- Certificados de prueba disponibles en portal SUNAT
