"""Agente Conciliador — Cruza facturas emitidas con pagos bancarios."""

from datetime import datetime, timedelta


class ConciliatorAgent:
    """Matches invoices with bank payments for reconciliation."""

    async def reconcile(self, documents: list[dict], payments: list[dict], tolerance: float = 0.05) -> dict:
        matched = []
        unmatched_docs = []
        unmatched_payments = []
        partial_matches = []

        remaining_payments = list(payments)

        for doc in documents:
            doc_total = doc.get("total", 0)
            doc_number = doc.get("full_number", "")
            doc_customer = doc.get("customer_name", "")
            doc_date = doc.get("issue_date", "")
            best_match = None
            best_score = 0

            for payment in remaining_payments:
                pay_amount = payment.get("amount", 0)
                pay_ref = payment.get("reference", "")
                pay_description = payment.get("description", "")
                pay_date = payment.get("date", "")

                score = 0

                # Amount matching (exact or within tolerance)
                amount_diff = abs(doc_total - pay_amount)
                if amount_diff == 0:
                    score += 50
                elif amount_diff <= doc_total * tolerance:
                    score += 30
                else:
                    continue  # Skip if amount is too different

                # Reference matching (doc number in payment reference)
                if doc_number and doc_number in pay_ref:
                    score += 30
                elif doc_number and any(part in pay_ref for part in doc_number.split("-")):
                    score += 15

                # Customer name matching
                if doc_customer and doc_customer.lower() in pay_description.lower():
                    score += 15
                elif doc_customer:
                    # Partial name match
                    doc_words = set(doc_customer.lower().split())
                    pay_words = set(pay_description.lower().split())
                    overlap = len(doc_words & pay_words) / max(len(doc_words), 1)
                    score += int(overlap * 10)

                # Date proximity (closer dates score higher)
                if doc_date and pay_date:
                    try:
                        d1 = datetime.fromisoformat(doc_date)
                        d2 = datetime.fromisoformat(pay_date)
                        days_diff = abs((d2 - d1).days)
                        if days_diff <= 3:
                            score += 10
                        elif days_diff <= 7:
                            score += 5
                    except (ValueError, TypeError):
                        pass

                if score > best_score:
                    best_score = score
                    best_match = payment

            if best_match and best_score >= 40:
                matched.append({
                    "document": doc,
                    "payment": best_match,
                    "score": best_score,
                    "confidence": min(best_score / 100, 0.99),
                    "status": "matched" if best_score >= 60 else "probable"
                })
                remaining_payments.remove(best_match)
            elif best_match and best_score >= 20:
                partial_matches.append({
                    "document": doc,
                    "payment": best_match,
                    "score": best_score,
                    "confidence": best_score / 100,
                    "status": "partial"
                })
            else:
                unmatched_docs.append(doc)

        unmatched_payments = remaining_payments

        total_docs = len(documents)
        total_matched = len(matched)
        match_rate = (total_matched / max(total_docs, 1)) * 100

        return {
            "matched": matched,
            "partial_matches": partial_matches,
            "unmatched_documents": unmatched_docs,
            "unmatched_payments": unmatched_payments,
            "summary": {
                "total_documents": total_docs,
                "total_payments": len(payments),
                "matched_count": total_matched,
                "partial_count": len(partial_matches),
                "unmatched_docs_count": len(unmatched_docs),
                "unmatched_payments_count": len(unmatched_payments),
                "match_rate": round(match_rate, 1),
            }
        }
