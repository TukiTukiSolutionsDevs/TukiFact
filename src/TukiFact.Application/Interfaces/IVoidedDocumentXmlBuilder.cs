using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

/// <summary>
/// Builds Comunicación de Baja (RA) UBL XML for SUNAT.
/// Schema: VoidedDocuments-1.0 — for invoice/credit-note/debit-note voids (types 01/07/08).
/// Boletas (03) require Resumen Diario (RC) which is a different schema (not handled here).
/// </summary>
public interface IVoidedDocumentXmlBuilder
{
    string BuildVoidedXml(VoidedDocument voided, Tenant tenant);
}
