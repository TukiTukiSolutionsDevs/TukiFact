using System.Globalization;
using System.Xml.Linq;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Builds Perception XML (UBL 2.0) for SUNAT.
/// IMPORTANT: Perceptions use UBL 2.0 (NOT 2.1 like invoices).
/// Namespace: urn:sunat:names:specification:ubl:peru:schema:xsd:Perception-1
/// Reference: R.S. 199-2019, Manual del Programador SUNAT
/// </summary>
public class PerceptionXmlBuilder : IPerceptionXmlBuilder
{
    private static readonly XNamespace PerNs = "urn:sunat:names:specification:ubl:peru:schema:xsd:Perception-1";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private static readonly XNamespace Sac = "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";

    public string BuildPerceptionXml(PerceptionDocument perception, Tenant tenant)
    {
        var root = new XElement(PerNs + "Perception",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", Cac.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ext", Ext.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "sac", Sac.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ds", Ds.NamespaceName),

            // UBL Extensions (placeholder for digital signature)
            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            // UBL Version 2.0 (NOT 2.1)
            new XElement(Cbc + "UBLVersionID", "2.0"),
            new XElement(Cbc + "CustomizationID", "1.0"),

            // Document ID: P001-00000001
            new XElement(Cbc + "ID", perception.FullNumber),

            // Issue date
            new XElement(Cbc + "IssueDate", perception.IssueDate.ToString("yyyy-MM-dd")),

            // Signature reference
            BuildSignatureReference(tenant),

            // Agent party (agente de percepción = vendedor/emisor)
            BuildAgentParty(tenant),

            // Receiver party (comprador = a quien se cobra la percepción)
            BuildReceiverParty(perception),

            // Perception regime (Catálogo 22)
            new XElement(Sac + "SUNATPerceptionSystemCode", perception.RegimeCode),
            new XElement(Sac + "SUNATPerceptionPercent", Fmt(perception.PerceptionPercent)),

            // Notes
            perception.Notes is not null
                ? new XElement(Cbc + "Note", perception.Notes)
                : null!,

            // Total amounts
            new XElement(Cbc + "TotalInvoiceAmount",
                new XAttribute("currencyID", perception.Currency),
                Fmt(perception.TotalInvoiceAmount)),
            new XElement(Sac + "SUNATTotalCashed",
                new XAttribute("currencyID", perception.Currency),
                Fmt(perception.TotalPerceived)),
            new XElement(Sac + "SUNATTotalPaid",
                new XAttribute("currencyID", perception.Currency),
                Fmt(perception.TotalCollected)),

            // Document references (facturas asociadas)
            perception.References.OrderBy(r => r.CollectionDate).Select(BuildPerceptionDocumentReference)
        );

        // Remove null elements
        root.Descendants()
            .Where(e => e.Value == null && !e.HasElements && !e.HasAttributes)
            .Remove();

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        using var sw = new StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    private XElement BuildSignatureReference(Tenant tenant)
    {
        return new XElement(Cac + "Signature",
            new XElement(Cbc + "ID", $"IDSign{tenant.Ruc}"),
            new XElement(Cac + "SignatoryParty",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID", tenant.Ruc)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", tenant.RazonSocial))),
            new XElement(Cac + "DigitalSignatureAttachment",
                new XElement(Cac + "ExternalReference",
                    new XElement(Cbc + "URI", "#SignatureSP"))));
    }

    private XElement BuildAgentParty(Tenant tenant)
    {
        return new XElement(Cac + "AgentParty",
            new XElement(Cac + "PartyIdentification",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", "6"),
                    tenant.Ruc)),
            new XElement(Cac + "PartyName",
                new XElement(Cbc + "Name", new XCData(tenant.RazonSocial))),
            new XElement(Cac + "PartyLegalEntity",
                new XElement(Cbc + "RegistrationName",
                    new XCData(tenant.RazonSocial))));
    }

    private XElement BuildReceiverParty(PerceptionDocument perception)
    {
        return new XElement(Cac + "ReceiverParty",
            new XElement(Cac + "PartyIdentification",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", perception.CustomerDocType),
                    perception.CustomerDocNumber)),
            new XElement(Cac + "PartyName",
                new XElement(Cbc + "Name", new XCData(perception.CustomerName))),
            new XElement(Cac + "PartyLegalEntity",
                new XElement(Cbc + "RegistrationName",
                    new XCData(perception.CustomerName))));
    }

    private XElement BuildPerceptionDocumentReference(PerceptionDocumentReference reference)
    {
        var refElement = new XElement(Sac + "SUNATPerceptionDocumentReference",
            // Referenced document
            new XElement(Cbc + "ID",
                new XAttribute("schemeID", reference.DocumentType),
                reference.DocumentNumber),
            new XElement(Cbc + "IssueDate", reference.DocumentDate.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "TotalInvoiceAmount",
                new XAttribute("currencyID", reference.InvoiceCurrency),
                Fmt(reference.InvoiceAmount)),

            // Collection info
            new XElement(Cac + "Payment",
                new XElement(Cbc + "ID", reference.CollectionNumber),
                new XElement(Cbc + "PaidAmount",
                    new XAttribute("currencyID", reference.InvoiceCurrency),
                    Fmt(reference.CollectionAmount)),
                new XElement(Cbc + "PaidDate", reference.CollectionDate.ToString("yyyy-MM-dd"))),

            // Perception amounts
            new XElement(Sac + "SUNATPerceptionAmount",
                new XAttribute("currencyID", "PEN"),
                Fmt(reference.PerceivedAmount)),
            new XElement(Sac + "SUNATNetTotalCashed",
                new XAttribute("currencyID", "PEN"),
                Fmt(reference.TotalCollectedAmount))
        );

        // Exchange rate (if different currency)
        if (reference.ExchangeRate.HasValue && reference.InvoiceCurrency != "PEN")
        {
            refElement.Add(new XElement(Cac + "ExchangeRate",
                new XElement(Cbc + "SourceCurrencyCode", reference.InvoiceCurrency),
                new XElement(Cbc + "TargetCurrencyCode", "PEN"),
                new XElement(Cbc + "CalculationRate", Fmt4(reference.ExchangeRate.Value)),
                new XElement(Cbc + "Date", reference.ExchangeRateDate?.ToString("yyyy-MM-dd")
                    ?? reference.CollectionDate.ToString("yyyy-MM-dd"))));
        }

        return refElement;
    }

    private static string Fmt(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
    private static string Fmt4(decimal value) => value.ToString("F4", CultureInfo.InvariantCulture);
}
