"""Agente Clasificador — Clasifica ítems (IGV, unidad medida, código SUNAT)."""

import re


# Common product keywords → SUNAT codes (simplified subset)
SUNAT_CODES = {
    "laptop": ("43211500", "NIU"), "computadora": ("43211500", "NIU"),
    "monitor": ("43211900", "NIU"), "teclado": ("43211700", "NIU"),
    "mouse": ("43211700", "NIU"), "impresora": ("43212100", "NIU"),
    "papel": ("14111500", "NIU"), "tinta": ("44103100", "NIU"),
    "servicio": (None, "ZZ"), "consultoría": (None, "ZZ"),
    "mantenimiento": (None, "ZZ"), "soporte": (None, "ZZ"),
    "licencia": (None, "ZZ"), "software": ("43230000", "ZZ"),
    "hosting": (None, "ZZ"), "diseño": (None, "ZZ"),
    "capacitación": (None, "ZZ"), "alquiler": (None, "ZZ"),
    "agua": ("50202301", "LTR"), "combustible": ("15101500", "LTR"),
    "arroz": ("50161500", "KGM"), "azúcar": ("50161600", "KGM"),
}

SERVICE_KEYWORDS = {
    "servicio", "consultoría", "asesoría", "soporte", "mantenimiento",
    "capacitación", "instalación", "configuración", "desarrollo",
    "diseño", "licencia", "hosting", "alquiler", "suscripción",
}


class ClassifierAgent:
    """Classifies items: IGV type, unit measure, SUNAT product code."""

    async def classify(self, item: dict) -> dict:
        description = (item.get("description") or "").lower().strip()
        unit_price = item.get("unit_price", 0)
        customer_doc_type = item.get("customer_doc_type")

        # Determine if it's a service or product
        is_service = any(kw in description for kw in SERVICE_KEYWORDS)
        unit_measure = "ZZ" if is_service else "NIU"

        # Find SUNAT code
        sunat_code = None
        for keyword, (code, um) in SUNAT_CODES.items():
            if keyword in description:
                sunat_code = code
                unit_measure = um
                break

        # Determine IGV type
        igv_type = "10"  # Default: gravado
        igv_name = "Gravado"
        reasoning = "Operación gravada estándar (IGV 18%)"

        # Check for free/promotional
        if unit_price == 0:
            igv_type = "21"
            igv_name = "Gratuito"
            reasoning = "Precio es 0 — clasificado como operación gratuita"
        # Check for potential exonerations (simplified)
        elif any(kw in description for kw in ["educación", "salud", "libro", "medicamento"]):
            igv_type = "20"
            igv_name = "Exonerado"
            reasoning = "Producto/servicio posiblemente exonerado de IGV"

        confidence = 0.85 if sunat_code else 0.65

        return {
            "igv_type": igv_type,
            "igv_type_name": igv_name,
            "suggested_unit_measure": unit_measure,
            "suggested_sunat_code": sunat_code,
            "confidence": confidence,
            "reasoning": reasoning,
        }
