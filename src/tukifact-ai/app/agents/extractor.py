"""Agente Extractor — Extrae datos de texto/OCR para crear comprobantes."""

import re
from typing import Any


class ExtractorAgent:
    """Extracts structured invoice data from raw text or OCR output."""

    RUC_PATTERN = re.compile(r"\b(10|15|16|17|20)\d{9}\b")
    DNI_PATTERN = re.compile(r"\b\d{8}\b")
    AMOUNT_PATTERN = re.compile(r"S/?\.?\s*([\d,]+\.?\d{0,2})")
    USD_PATTERN = re.compile(r"\$\s*([\d,]+\.?\d{0,2})")
    DATE_PATTERN = re.compile(r"\b(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{2,4})\b")

    async def extract(self, text: str, source_type: str = "text") -> dict:
        raw_fields: dict[str, Any] = {}
        items: list[dict] = []
        confidence = 0.5

        # Extract RUC
        ruc_matches = self.RUC_PATTERN.findall(text)
        customer_doc_number = None
        customer_doc_type = None
        if ruc_matches:
            # First RUC might be the emitter, second is customer
            rucs = list(set(self.RUC_PATTERN.findall(text)))
            raw_fields["found_rucs"] = [m + text[text.find(m):text.find(m)+11] for m in ruc_matches[:3]]
            if len(rucs) >= 1:
                # Heuristic: take the last RUC found (usually the customer)
                full_rucs = self.RUC_PATTERN.findall(text)
                customer_doc_number = full_rucs[-1] if full_rucs else None
                customer_doc_type = "6"
                confidence += 0.1

        # Extract DNI if no RUC
        if not customer_doc_number:
            dnis = self.DNI_PATTERN.findall(text)
            if dnis:
                customer_doc_number = dnis[0]
                customer_doc_type = "1"
                raw_fields["found_dnis"] = dnis[:3]

        # Extract customer name (heuristic: text after "razón social" or "cliente")
        customer_name = None
        for pattern in [r"(?:razón\s+social|razon\s+social|cliente|señor|señores?)\s*[:\-]?\s*(.+)", r"(?:RAZÓN\s+SOCIAL|CLIENTE)\s*[:\-]?\s*(.+)"]:
            match = re.search(pattern, text, re.IGNORECASE)
            if match:
                customer_name = match.group(1).strip()[:200]
                confidence += 0.1
                break

        # Extract address
        customer_address = None
        addr_match = re.search(r"(?:dirección|direccion|domicilio)\s*[:\-]?\s*(.+)", text, re.IGNORECASE)
        if addr_match:
            customer_address = addr_match.group(1).strip()[:300]

        # Extract amounts
        amounts = [float(m.replace(",", "")) for m in self.AMOUNT_PATTERN.findall(text)]
        usd_amounts = [float(m.replace(",", "")) for m in self.USD_PATTERN.findall(text)]
        currency = "USD" if usd_amounts and not amounts else "PEN"
        all_amounts = usd_amounts if currency == "USD" else amounts

        total = max(all_amounts) if all_amounts else None
        raw_fields["found_amounts"] = all_amounts[:10]

        # Determine document type
        document_type = None
        text_upper = text.upper()
        if "FACTURA" in text_upper:
            document_type = "01"
        elif "BOLETA" in text_upper:
            document_type = "03"
        elif "NOTA DE CRÉDITO" in text_upper or "NOTA DE CREDITO" in text_upper:
            document_type = "07"

        # Extract line items (simplified: look for description + amount patterns)
        line_pattern = re.compile(r"(\d+)\s+(.{10,80}?)\s+(\d+\.?\d*)\s+([\d,.]+)")
        for match in line_pattern.finditer(text):
            try:
                items.append({
                    "description": match.group(2).strip(),
                    "quantity": float(match.group(3)),
                    "unit_price": float(match.group(4).replace(",", "")),
                    "igv_type": "10",
                    "unit_measure": "NIU",
                })
                confidence += 0.05
            except ValueError:
                continue

        confidence = min(confidence, 0.95)

        return {
            "document_type": document_type,
            "customer_doc_number": customer_doc_number,
            "customer_name": customer_name,
            "customer_address": customer_address,
            "items": items,
            "total": total,
            "currency": currency,
            "confidence": round(confidence, 2),
            "raw_fields": raw_fields,
        }
