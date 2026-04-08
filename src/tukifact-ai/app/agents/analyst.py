"""Agente Analista — Genera insights y análisis inteligentes de facturación."""


class AnalystAgent:
    """Analyzes invoicing data and generates insights."""

    async def analyze(self, dashboard_data: dict) -> dict:
        today = dashboard_data.get("today", {})
        this_month = dashboard_data.get("this_month", {})
        this_year = dashboard_data.get("this_year", {})
        by_type = dashboard_data.get("by_type", [])
        by_status = dashboard_data.get("by_status", [])
        monthly_sales = dashboard_data.get("monthly_sales", [])

        insights = []
        recommendations = []
        alerts = []

        # Revenue analysis
        month_amount = this_month.get("total_amount", 0)
        month_docs = this_month.get("total_documents", 0)
        year_amount = this_year.get("total_amount", 0)

        if month_amount > 0:
            avg_per_doc = month_amount / max(month_docs, 1)
            insights.append({
                "type": "revenue",
                "title": "Ticket promedio",
                "value": f"S/ {avg_per_doc:,.2f}",
                "description": f"Promedio por documento este mes ({month_docs} documentos)"
            })

        # Trend analysis
        if len(monthly_sales) >= 2:
            current = monthly_sales[-1].get("total", 0)
            previous = monthly_sales[-2].get("total", 0)
            if previous > 0:
                growth = ((current - previous) / previous) * 100
                trend = "crecimiento" if growth > 0 else "decrecimiento"
                insights.append({
                    "type": "trend",
                    "title": f"{trend.capitalize()} mensual",
                    "value": f"{growth:+.1f}%",
                    "description": f"Comparado con el mes anterior (S/ {previous:,.2f} → S/ {current:,.2f})"
                })
                if growth < -20:
                    alerts.append(f"Caída significativa en ventas: {growth:.1f}% respecto al mes anterior")

        # Rejection analysis
        rejected = 0
        total = 0
        for s in by_status:
            total += s.get("count", 0)
            if s.get("status") == "rejected":
                rejected = s.get("count", 0)

        if total > 0 and rejected > 0:
            rejection_rate = (rejected / total) * 100
            if rejection_rate > 5:
                alerts.append(f"Tasa de rechazo alta: {rejection_rate:.1f}% ({rejected}/{total} documentos)")
                recommendations.append("Revisar los motivos de rechazo de SUNAT. Usar el Agente Validador antes de emitir.")

        # Document type distribution
        if by_type:
            facturas = sum(t.get("count", 0) for t in by_type if t.get("document_type") == "01")
            boletas = sum(t.get("count", 0) for t in by_type if t.get("document_type") == "03")
            ncs = sum(t.get("count", 0) for t in by_type if t.get("document_type") == "07")

            if facturas > 0 and boletas == 0:
                recommendations.append("Solo emites facturas. Si tienes clientes finales (personas), considera emitir boletas también.")

            if ncs > 0 and facturas > 0:
                nc_rate = (ncs / facturas) * 100
                if nc_rate > 10:
                    alerts.append(f"Alto porcentaje de notas de crédito: {nc_rate:.1f}% de facturas tienen NC asociada")

        # IGV projection
        month_igv = this_month.get("total_igv", 0)
        if month_igv > 0:
            insights.append({
                "type": "tax",
                "title": "IGV del mes",
                "value": f"S/ {month_igv:,.2f}",
                "description": "IGV total a declarar este periodo"
            })

        # General recommendations
        if month_docs < 5:
            recommendations.append("Bajo volumen de documentos. Verifica que todas las ventas estén siendo facturadas.")

        if not alerts:
            insights.append({
                "type": "health",
                "title": "Estado general",
                "value": "Saludable",
                "description": "No se detectaron problemas en tu facturación"
            })

        return {
            "insights": insights,
            "recommendations": recommendations,
            "alerts": alerts,
            "summary": f"Este mes: {month_docs} documentos por S/ {month_amount:,.2f}. "
                       f"Año acumulado: S/ {year_amount:,.2f}."
        }
