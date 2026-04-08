"""Agente Copiloto — Asistente contextual de facturación electrónica."""

from typing import Optional


# Knowledge base: SUNAT electronic invoicing rules (simplified RAG)
KNOWLEDGE_BASE = {
    "factura": {
        "description": "Comprobante de pago que se emite a personas jurídicas (empresas con RUC). Tipo 01.",
        "rules": [
            "Requiere RUC del cliente (tipo documento 6)",
            "Serie empieza con F (ej: F001, F002)",
            "Debe tener al menos un ítem",
            "IGV de 18% para operaciones gravadas",
            "Plazo de envío a SUNAT: máximo 7 días calendario desde la emisión"
        ]
    },
    "boleta": {
        "description": "Comprobante de pago que se emite a personas naturales (consumidores finales). Tipo 03.",
        "rules": [
            "No requiere RUC — acepta DNI o sin documento",
            "Serie empieza con B (ej: B001)",
            "Si el monto supera S/ 700, debe identificarse al cliente",
            "Se envían a SUNAT via Resumen Diario (no individual)",
            "No genera derecho a crédito fiscal para el comprador"
        ]
    },
    "nota_credito": {
        "description": "Documento que modifica (reduce) una factura o boleta emitida. Tipo 07.",
        "rules": [
            "Debe referenciar el documento original",
            "Motivos válidos: anulación, devolución, descuento, corrección",
            "Usa la misma serie que el documento original (con tipo 07)",
            "El código de motivo es obligatorio (Catálogo 09 SUNAT)"
        ]
    },
    "nota_debito": {
        "description": "Documento que modifica (aumenta) una factura o boleta emitida. Tipo 08.",
        "rules": [
            "Debe referenciar el documento original",
            "Motivos: intereses por mora, aumento en el valor, penalidades",
            "Código de motivo obligatorio (Catálogo 10 SUNAT)"
        ]
    },
    "igv": {
        "description": "Impuesto General a las Ventas — 18% en Perú.",
        "rules": [
            "Gravado (10): operaciones normales, 18%",
            "Exonerado (20): ciertos productos/servicios liberados del IGV",
            "Inafecto (30): operaciones fuera del ámbito del IGV",
            "Gratuito (21): transferencias gratuitas, no genera IGV cobrable"
        ]
    },
    "ruc": {
        "description": "Registro Único de Contribuyente — identificador tributario en Perú.",
        "rules": [
            "11 dígitos",
            "Empieza con 10 (persona natural) o 20 (persona jurídica)",
            "También: 15, 16, 17 para otros tipos de contribuyentes",
            "Verificar estado activo en SUNAT antes de emitir"
        ]
    },
    "serie": {
        "description": "Identificador de punto de emisión. 4 caracteres alfanuméricos.",
        "rules": [
            "Facturas: empieza con F (F001, F002...)",
            "Boletas: empieza con B (B001, B002...)",
            "NC/ND: misma letra que el documento de referencia",
            "El correlativo es secuencial y no se puede repetir"
        ]
    },
    "comunicacion_baja": {
        "description": "Documento para anular comprobantes ya enviados a SUNAT.",
        "rules": [
            "Se puede anular hasta 7 días después de la emisión",
            "Genera un ticket que SUNAT procesa asincrónicamente",
            "El número de ticket es: RA-YYYYMMDD-NNN",
            "Solo se pueden anular documentos en estado 'aceptado'"
        ]
    },
    "certificado_digital": {
        "description": "Certificado X.509 para firma digital de comprobantes electrónicos.",
        "rules": [
            "Formatos aceptados: PFX/P12 y PEM",
            "Debe estar vigente (no expirado)",
            "Emitido por una entidad certificadora autorizada por SUNAT",
            "Se usa para firmar el XML antes de enviarlo a SUNAT"
        ]
    },
    "plazo_envio": {
        "description": "Plazos para enviar comprobantes a SUNAT.",
        "rules": [
            "Facturas: hasta 7 días calendario desde emisión",
            "Boletas: envío consolidado via Resumen Diario",
            "NC/ND: hasta 7 días desde emisión",
            "Comunicación de Baja: hasta 7 días desde emisión del documento original"
        ]
    }
}


class CopilotAgent:
    """Contextual chat assistant for electronic invoicing."""

    async def chat(self, message: str, context: dict | None = None) -> dict:
        message_lower = message.lower()

        # Find relevant topics
        relevant_topics = []
        for key, info in KNOWLEDGE_BASE.items():
            key_variants = [key, key.replace("_", " ")]
            if any(variant in message_lower for variant in key_variants):
                relevant_topics.append((key, info))

        # Keyword matching for common questions
        if any(w in message_lower for w in ["cómo emitir", "como emitir", "emitir factura", "crear factura"]):
            return self._how_to_emit(context)

        if any(w in message_lower for w in ["anular", "baja", "cancelar factura"]):
            return self._how_to_void()

        if any(w in message_lower for w in ["nota de crédito", "nota credito", "devolver", "devolución"]):
            return self._how_to_credit_note()

        if any(w in message_lower for w in ["igv", "impuesto", "18%", "gravado", "exonerado"]):
            return self._explain_igv()

        if any(w in message_lower for w in ["qué es", "que es", "qué significa", "explicar"]):
            if relevant_topics:
                topic_key, topic = relevant_topics[0]
                return {
                    "response": f"**{topic_key.replace('_', ' ').title()}**: {topic['description']}\n\n**Reglas clave:**\n" +
                                "\n".join(f"- {r}" for r in topic["rules"]),
                    "sources": [topic_key],
                    "confidence": 0.9,
                    "suggestions": ["¿Qué más quieres saber?", "¿Necesitas ayuda para emitir?"]
                }

        # General response with context
        if relevant_topics:
            response_parts = []
            for key, info in relevant_topics[:3]:
                response_parts.append(f"**{key.replace('_', ' ').title()}**: {info['description']}")
                response_parts.append("Reglas: " + "; ".join(info["rules"][:3]))

            return {
                "response": "\n\n".join(response_parts),
                "sources": [k for k, _ in relevant_topics],
                "confidence": 0.75,
                "suggestions": self._suggest_followups(relevant_topics)
            }

        # Fallback
        return {
            "response": "Puedo ayudarte con:\n"
                        "- **Emisión** de facturas, boletas, NC y ND\n"
                        "- **Reglas SUNAT** para comprobantes electrónicos\n"
                        "- **IGV** y tipos de afectación\n"
                        "- **Anulación** de documentos\n"
                        "- **Series** y correlativos\n"
                        "- **Certificado digital** y firma\n\n"
                        "¿Sobre qué tema necesitas ayuda?",
            "sources": [],
            "confidence": 0.3,
            "suggestions": [
                "¿Cómo emitir una factura?",
                "¿Cuáles son los tipos de IGV?",
                "¿Cómo anular un comprobante?",
                "¿Qué es una nota de crédito?"
            ]
        }

    def _how_to_emit(self, context: dict | None = None) -> dict:
        return {
            "response": "**Para emitir un comprobante electrónico:**\n\n"
                        "1. Asegúrate de tener una **serie** creada (ej: F001 para facturas)\n"
                        "2. Ve a **Emitir Comprobante** en el menú\n"
                        "3. Selecciona el **tipo** (Factura o Boleta)\n"
                        "4. Ingresa los datos del **cliente** (RUC para facturas, DNI para boletas)\n"
                        "5. Agrega los **ítems** con descripción, cantidad y precio\n"
                        "6. El sistema calcula **IGV** automáticamente\n"
                        "7. Haz clic en **Emitir** — el XML se genera, firma y envía a SUNAT\n\n"
                        "**Tip:** Usa el Agente Validador (`/v1/ai/validate`) para verificar antes de emitir.",
            "sources": ["factura", "boleta", "serie"],
            "confidence": 0.95,
            "suggestions": ["¿Cómo crear una serie?", "¿Qué datos necesito del cliente?"]
        }

    def _how_to_void(self) -> dict:
        return {
            "response": "**Para anular un comprobante:**\n\n"
                        "1. Abre el **detalle** del documento que quieres anular\n"
                        "2. El documento debe estar en estado **Aceptado**\n"
                        "3. Haz clic en el botón **Anular**\n"
                        "4. Ingresa la **razón** de anulación\n"
                        "5. Se genera una **Comunicación de Baja** (RA-YYYYMMDD-NNN)\n"
                        "6. SUNAT procesa la baja **asincrónicamente**\n\n"
                        "**Importante:** Solo tienes **7 días** desde la emisión para anular.",
            "sources": ["comunicacion_baja"],
            "confidence": 0.95,
            "suggestions": ["¿Puedo anular una boleta?", "¿Qué pasa si pasaron los 7 días?"]
        }

    def _how_to_credit_note(self) -> dict:
        return {
            "response": "**Para emitir una Nota de Crédito:**\n\n"
                        "1. Abre el **detalle** de la factura/boleta original\n"
                        "2. Haz clic en **Emitir Nota de Crédito**\n"
                        "3. Selecciona el **motivo** (devolución, descuento, anulación, etc.)\n"
                        "4. Los **ítems** se pre-cargan del documento original\n"
                        "5. Ajusta cantidades/precios si es una devolución **parcial**\n"
                        "6. Emite — la NC se envía a SUNAT referenciando la factura original\n\n"
                        "**Motivos comunes:** 01-Anulación, 06-Devolución total, 07-Devolución por ítem",
            "sources": ["nota_credito"],
            "confidence": 0.95,
            "suggestions": ["¿Cuál es la diferencia entre NC y anulación?"]
        }

    def _explain_igv(self) -> dict:
        return {
            "response": "**Tipos de IGV en Perú:**\n\n"
                        "| Código | Tipo | Descripción | IGV |\n"
                        "|--------|------|-------------|-----|\n"
                        "| 10 | **Gravado** | Operación normal | 18% |\n"
                        "| 20 | **Exonerado** | Productos/servicios exonerados por ley | 0% |\n"
                        "| 30 | **Inafecto** | Fuera del ámbito del IGV | 0% |\n"
                        "| 21 | **Gratuito** | Transferencias gratuitas | 0% |\n\n"
                        "El **18%** se aplica sobre el valor de venta (precio sin IGV).\n"
                        "Ejemplo: Producto a S/ 100 + IGV = S/ 118.",
            "sources": ["igv"],
            "confidence": 0.95,
            "suggestions": ["¿Qué productos son exonerados?", "¿Cómo registro una operación gratuita?"]
        }

    def _suggest_followups(self, topics: list) -> list[str]:
        suggestions = []
        topic_keys = [k for k, _ in topics]
        if "factura" in topic_keys:
            suggestions.append("¿Cómo emitir una factura?")
        if "igv" in topic_keys:
            suggestions.append("¿Qué productos son exonerados?")
        if "serie" in topic_keys:
            suggestions.append("¿Cómo crear una nueva serie?")
        return suggestions[:3] if suggestions else ["¿Necesitas ayuda con algo más?"]
