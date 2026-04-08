"""Agente Validador — Valida comprobantes antes de emitirlos."""

import re


class ValidatorAgent:
    """Validates documents before emission, checking SUNAT rules."""

    VALID_DOC_TYPES = {"01", "03", "07", "08"}
    VALID_CURRENCIES = {"PEN", "USD"}
    VALID_IGV_TYPES = {"10", "20", "30", "21"}

    async def validate(self, document: dict) -> dict:
        errors: list[str] = []
        warnings: list[str] = []
        suggestions: list[str] = []

        # Document type
        doc_type = document.get("document_type", "")
        if doc_type not in self.VALID_DOC_TYPES:
            errors.append(f"Tipo de documento inválido: '{doc_type}'. Válidos: 01, 03, 07, 08")

        # Serie validation
        serie = document.get("serie", "")
        if len(serie) != 4:
            errors.append(f"Serie debe tener 4 caracteres, recibido: '{serie}'")
        elif doc_type == "01" and not serie.startswith("F"):
            errors.append(f"Factura (01) debe usar serie que empieza con F, recibido: '{serie}'")
        elif doc_type == "03" and not serie.startswith("B"):
            errors.append(f"Boleta (03) debe usar serie que empieza con B, recibido: '{serie}'")

        # Customer validation
        cust_doc_type = document.get("customer_doc_type", "")
        cust_doc_number = document.get("customer_doc_number", "")

        if doc_type == "01" and cust_doc_type != "6":
            errors.append("Facturas requieren RUC (tipo doc 6) del cliente")

        if cust_doc_type == "6":  # RUC
            if not re.match(r"^(10|15|16|17|20)\d{9}$", cust_doc_number):
                errors.append(f"RUC inválido: '{cust_doc_number}'. Debe empezar con 10, 15, 16, 17 o 20 y tener 11 dígitos")
        elif cust_doc_type == "1":  # DNI
            if not re.match(r"^\d{8}$", cust_doc_number):
                errors.append(f"DNI inválido: '{cust_doc_number}'. Debe tener 8 dígitos")

        if not document.get("customer_name", "").strip():
            errors.append("Nombre del cliente es requerido")

        # Currency
        currency = document.get("currency", "PEN")
        if currency not in self.VALID_CURRENCIES:
            errors.append(f"Moneda inválida: '{currency}'. Válidas: PEN, USD")

        # Items
        items = document.get("items", [])
        if not items:
            errors.append("Debe incluir al menos un ítem")

        for i, item in enumerate(items, 1):
            desc = item.get("description", "")
            if not desc.strip():
                errors.append(f"Ítem {i}: descripción requerida")
            if len(desc) > 500:
                warnings.append(f"Ítem {i}: descripción muy larga ({len(desc)} chars), SUNAT permite máximo 500")

            qty = item.get("quantity", 0)
            if qty <= 0:
                errors.append(f"Ítem {i}: cantidad debe ser mayor a 0")

            price = item.get("unit_price", 0)
            if price < 0:
                errors.append(f"Ítem {i}: precio no puede ser negativo")
            if price == 0:
                warnings.append(f"Ítem {i}: precio es 0. ¿Es una operación gratuita?")
                if item.get("igv_type") != "21":
                    suggestions.append(f"Ítem {i}: si es gratuito, usar igv_type='21'")

            igv = item.get("igv_type", "10")
            if igv not in self.VALID_IGV_TYPES:
                errors.append(f"Ítem {i}: tipo IGV inválido '{igv}'. Válidos: 10 (gravado), 20 (exonerado), 30 (inafecto), 21 (gratuito)")

        # Business rules
        if doc_type == "03" and cust_doc_type == "6":
            suggestions.append("Boletas normalmente usan DNI (tipo 1) o Sin documento (tipo 0), no RUC")

        total_value = sum(
            (item.get("quantity", 0) * item.get("unit_price", 0)) for item in items
        )
        if doc_type == "03" and total_value > 700:
            warnings.append(f"Boletas mayores a S/ 700 requieren identificar al cliente (total estimado: S/ {total_value:.2f})")

        return {
            "is_valid": len(errors) == 0,
            "errors": errors,
            "warnings": warnings,
            "suggestions": suggestions,
        }
