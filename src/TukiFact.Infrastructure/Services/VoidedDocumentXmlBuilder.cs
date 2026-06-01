using System.Text.Json;
using System.Xml.Linq;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Builds Comunicación de Baja (RA) UBL XML — schema VoidedDocuments-1.0.
/// Reference: SUNAT Manual del Programador / R.S. 097-2012.
/// Applies to types 01 (Factura), 07 (Nota de Crédito), 08 (Nota de Débito).
/// </summary>
public class VoidedDocumentXmlBuilder : IVoidedDocumentXmlBuilder
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly XNamespace VoidNs = "urn:sunat:names:specification:ubl:peru:schema:xsd:VoidedDocuments-1";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private static readonly XNamespace Sac = "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";

    public string BuildVoidedXml(VoidedDocument voided, Tenant tenant)
    {
        var items = JsonSerializer.Deserialize<List<VoidedLine>>(voided.ItemsJson, JsonOpts)
                    ?? throw new InvalidOperationException("ItemsJson vacío o inválido");

        if (items.Count == 0)
            throw new InvalidOperationException("VoidedDocument sin líneas a anular");

        var root = new XElement(VoidNs + "VoidedDocuments",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", Cac.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ext", Ext.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "sac", Sac.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ds", Ds.NamespaceName),

            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            new XElement(Cbc + "UBLVersionID", "2.0"),
            new XElement(Cbc + "CustomizationID", "1.0"),
            new XElement(Cbc + "ID", voided.TicketNumber),
            new XElement(Cbc + "ReferenceDate", voided.ReferenceDate.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueDate", voided.IssueDate.ToString("yyyy-MM-dd")),

            BuildSignatureReference(tenant),
            BuildSupplierParty(tenant),

            items.Select((it, idx) => BuildVoidedLine(idx + 1, it))
        );

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        using var sw = new Utf8StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    private static XElement BuildSignatureReference(Tenant tenant) =>
        new(Cac + "Signature",
            new XElement(Cbc + "ID", $"IDSign{tenant.Ruc}"),
            new XElement(Cac + "SignatoryParty",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID", tenant.Ruc)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", tenant.RazonSocial))),
            new XElement(Cac + "DigitalSignatureAttachment",
                new XElement(Cac + "ExternalReference",
                    new XElement(Cbc + "URI", "#SignatureSP"))));

    private static XElement BuildSupplierParty(Tenant tenant) =>
        new(Cac + "AccountingSupplierParty",
            new XElement(Cbc + "CustomerAssignedAccountID", tenant.Ruc),
            new XElement(Cbc + "AdditionalAccountID", "6"), // 6 = RUC
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", new XCData(tenant.RazonSocial)))));

    private static XElement BuildVoidedLine(int lineId, VoidedLine line) =>
        new(Sac + "VoidedDocumentsLine",
            new XElement(Cbc + "LineID", lineId),
            new XElement(Cbc + "DocumentTypeCode", line.DocumentType),
            new XElement(Sac + "DocumentSerialID", line.Serie),
            new XElement(Sac + "DocumentNumberID", line.Correlative),
            new XElement(Sac + "VoidReasonDescription", new XCData(line.Reason)));

    private record VoidedLine(
        string DocumentType,
        string Serie,
        long Correlative,
        string FullNumber,
        string Reason);
}
