# 10 - Diseño de Agentes IA

## Filosofía
Los agentes NO son chatbots decorativos. Son ESPECIALISTAS que entienden la operación tributaria peruana y pueden ejecutar acciones reales dentro del sistema. Cada agente tiene skills, tools, y knowledge base específicos.

## Arquitectura del Orquestador

```
┌─────────────────────────────────────────┐
│         ORQUESTADOR DELIBERATIVO        │
│    (Router + Context + Memory + Loop)   │
└────────┬────────────────────────────────┘
         │
    ┌────┴────┐
    │ ROUTER  │ → Analiza intent del usuario,
    │  + RAG  │   rol, contexto de conversación
    └────┬────┘
         │
   ┌─────┴──────────────────────────────────────┐
   │                                             │
   ▼           ▼           ▼           ▼         ▼
┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐
│EMISOR│  │TRIBUT│  │CONTA │  │LOGÍS │  │AUDIT │
│Agent │  │Agent │  │Agent │  │Agent │  │Agent │
└──────┘  └──────┘  └──────┘  └──────┘  └──────┘
   MVP      Fase 8    Fase 8   Fase 8    Fase 8
```

## BYOK (Bring Your Own Key)

```python
# Router de proveedores IA
class AIProviderRouter:
    providers = {
        "openai": OpenAIProvider,
        "anthropic": AnthropicProvider,
        # Fase futura:
        # "gemini": GeminiProvider,
        # "groq": GroqProvider,
        # "deepseek": DeepSeekProvider,
    }
    
    def get_client(self, tenant_id: str) -> AIClient:
        config = get_tenant_ai_config(tenant_id)
        provider = self.providers[config.provider]
        return provider(api_key=config.api_key, model=config.model)
```

Cada tenant configura:
- Proveedor (OpenAI, Anthropic)
- API Key propia
- Modelo preferido (gpt-4o, claude-sonnet, etc.)

Si no configura BYOK, usa el provider por defecto de TukiFact (con límites según plan).

---

## Agente Emisor (MVP)

### Rol
Especialista en emisión de documentos electrónicos, errores de SUNAT, y asistencia operativa.

### Skills
| Skill | Descripción | Tool asociado |
|-------|-------------|---------------|
| Análisis de CDR | Interpreta códigos de error/observación de SUNAT | `query_cdr(document_id)` |
| Asistencia de emisión | Guía paso a paso para emitir documentos | `get_document_template(type)` |
| Consulta de documentos | Busca documentos por criterios | `search_documents(filters)` |
| Consulta de catálogos | Explica códigos SUNAT | `query_catalog(number, code)` |
| Estado de resúmenes | Verifica boletas pendientes | `get_pending_summaries()` |
| Reglas de series | Explica qué serie usar | `get_series_rules(doc_type)` |
| Validación RUC | Verifica RUC en padrón | `validate_ruc(ruc)` |

### Knowledge Base (RAG)
Documentos indexados para retrieval:
- Reglas de validación SUNAT (300+ reglas con códigos de error)
- Catálogos completos (58+)
- Errores comunes y soluciones
- Guías de elaboración XML UBL 2.1 de SUNAT
- FAQ de CPE (cpe.sunat.gob.pe)
- Resoluciones de superintendencia vigentes

### Tools (Function Calling)
```python
# Tools disponibles para el Agente Emisor
tools = [
    {
        "name": "query_cdr",
        "description": "Consulta el CDR de un documento para analizar errores",
        "parameters": {
            "document_id": "UUID del documento"
        }
    },
    {
        "name": "search_documents",
        "description": "Busca documentos emitidos",
        "parameters": {
            "type": "Tipo de documento (01, 03, 07, 08)",
            "status": "Estado SUNAT (accepted, rejected, etc.)",
            "date_from": "Fecha desde",
            "date_to": "Fecha hasta",
            "receptor_ruc": "RUC del receptor"
        }
    },
    {
        "name": "query_catalog",
        "description": "Consulta un catálogo SUNAT",
        "parameters": {
            "catalog_number": "Número de catálogo",
            "search": "Texto o código a buscar"
        }
    },
    {
        "name": "get_pending_summaries",
        "description": "Obtiene boletas pendientes de enviar en resumen diario",
        "parameters": {}
    },
    {
        "name": "get_document_stats",
        "description": "Obtiene estadísticas de documentos emitidos",
        "parameters": {
            "period": "Periodo (mes actual, último mes, etc.)"
        }
    },
    {
        "name": "validate_ruc",
        "description": "Valida un RUC contra el padrón de SUNAT",
        "parameters": {
            "ruc": "Número de RUC a validar"
        }
    },
    {
        "name": "get_exchange_rate",
        "description": "Obtiene el tipo de cambio SUNAT del día",
        "parameters": {
            "date": "Fecha (opcional, default hoy)"
        }
    }
]
```

### Ejemplo de Loop Deliberativo

```
Usuario: "¿Por qué me rechazaron la factura F001-345?"

Orquestador:
  1. Detecta intent: ERROR_ANALYSIS
  2. Routea a: Agente Emisor
  
Agente Emisor:
  1. THINK: Necesito consultar el CDR de este documento
  2. ACTION: query_cdr(document_id="...buscar por F001-345...")
  3. OBSERVE: CDR tiene código 2329 "Tipo de afectación IGV no válido para ítem 3"
  4. THINK: El código de afectación del ítem 3 no es válido. Debo ver qué código se usó.
  5. ACTION: search_documents(full_number="F001-345")
  6. OBSERVE: Ítem 3 tiene igv_type="18" que no existe
  7. THINK: El código 18 no existe en Cat. 07. Los válidos cercanos son 17 (IVAP) y 10 (Gravado).
  8. RESPOND: "La factura F001-345 fue rechazada porque el ítem 3 tiene 
     código de afectación IGV '18' que no existe en el catálogo SUNAT. 
     Los códigos válidos son:
     - 10: Gravado - Operación Onerosa
     - 17: Gravado - IVAP
     - 20: Exonerado
     - 30: Inafecto
     ¿Es una operación gravada normal? Entonces debe ser código 10.
     ¿Querés que prepare una nueva factura corregida?"

Usuario: "Sí, con código 10"

Agente Emisor:
  1. THINK: El usuario quiere re-emitir con código 10
  2. ACTION: Preparar datos corregidos y confirmar
  3. RESPOND: "Perfecto. Voy a preparar la factura F001-346 con los mismos 
     datos pero corrigiendo el ítem 3 a código 10 (Gravado). 
     ¿Confirmo la emisión?"
```

---

## Agentes Futuros (Post-MVP)

### Agente Tributario (Fase 8)
- IGV, ISC, IVAP, detracciones, retenciones, percepciones
- "¿Esta operación tiene detracción?" → analiza bien/servicio
- "¿Cuánto IGV debo este mes?" → cálculo crédito fiscal

### Agente Contable (Fase 8)
- SIRE, Registro de Compras/Ventas, conciliaciones
- "¿Mi registro coincide con SUNAT?" → cruza datos
- "Generame el reporte para el PDT" → totales desglosados

### Agente Logístico (Fase 8)
- GRE Remitente, Transportista, Eventos, UBIGEO
- "Necesito guía Lima a Arequipa" → genera GRE
- "El camión se malogró" → GRE por Eventos

### Agente Auditor (Fase 8)
- Trazabilidad, anomalías, compliance
- "¿Hay facturas a RUCs no habidos?" → cruza con padrón
- "Operaciones sospechosas del mes" → detección de patrones

### Agente Analítico (Fase 8)
- Dashboards, métricas, proyecciones
- "Ticket promedio este trimestre" → análisis comparativo
- "¿Cuánto IGV voy a deber?" → proyección

---

## Infraestructura Técnica del AI Service

```
FastAPI (Python)
├── app/
│   ├── main.py              # App + WebSocket endpoint
│   ├── config.py             # Settings + BYOK config
│   ├── providers/
│   │   ├── base.py           # AIProvider interface
│   │   ├── openai.py         # OpenAI implementation
│   │   └── anthropic.py      # Anthropic implementation
│   ├── agents/
│   │   ├── base.py           # BaseAgent with tool execution
│   │   ├── emisor.py         # Agente Emisor
│   │   └── router.py         # Intent router
│   ├── tools/
│   │   ├── cdr.py            # query_cdr tool
│   │   ├── documents.py      # search_documents tool
│   │   ├── catalogs.py       # query_catalog tool
│   │   ├── summaries.py      # get_pending_summaries tool
│   │   ├── ruc.py            # validate_ruc tool
│   │   └── exchange_rate.py  # get_exchange_rate tool
│   ├── rag/
│   │   ├── indexer.py        # Index SUNAT docs
│   │   ├── retriever.py      # Retrieve relevant context
│   │   └── knowledge/        # Knowledge base files
│   │       ├── reglas_validacion.md
│   │       ├── catalogos/
│   │       ├── errores_cdr.md
│   │       └── faq_sunat.md
│   └── db/
│       └── readonly.py       # SQLAlchemy read-only connection
├── Dockerfile
└── requirements.txt
```

## WebSocket Protocol

```json
// Cliente → Servidor
{
  "type": "message",
  "content": "¿Por qué me rechazaron la factura F001-345?",
  "conversation_id": "uuid"
}

// Servidor → Cliente (streaming)
{
  "type": "thinking",
  "content": "Analizando el CDR del documento..."
}

{
  "type": "tool_call",
  "tool": "query_cdr",
  "params": {"document_id": "..."}
}

{
  "type": "message",
  "content": "La factura fue rechazada porque...",
  "conversation_id": "uuid"
}
```
