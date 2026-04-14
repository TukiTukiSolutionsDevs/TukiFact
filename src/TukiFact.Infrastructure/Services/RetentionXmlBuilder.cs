using System.Globalization;
using System.Xml.Linq;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Builds Retention XML (UBL 2.0) for SUNAT.
/// IMPORTANT: Retentions use UBL 2.0 (NOT 2.1 like invoices).
/// Namespace: urn:sunat:names:specification:ubl:peru:schema:xsd:Retention-1
/// Reference: R.S. 274-2015, Manual del Programador SUNAT
/// </summary>
public class RetentionXmlBuilder : IRetentionXmlBuilder
{
    private static readonly XNamespace RetNs = "urn:sunat:names:specification:ubl:peru:schema:xsd:Retention-1";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private static readonly XNamespace Sac = "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";

    public string BuildRetentionXml(RetentionDocument retention, Tenant tenant)
    {
        var root = new XElement(RetNs + "Retention",
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

            // Document ID: R001-00000001
            new XElement(Cbc + "ID", retention.FullNumber),

            // Issue date
            new XElement(Cbc + "IssueDate", retention.IssueDate.ToString("yyyy-MM-dd")),

            // Signature reference
            BuildSignatureReference(tenant),

            // Agent party (agente de retención = emisor)
            BuildAgentParty(tenant),

            // Receiver party (proveedor = a quien se retiene)
            BuildReceiverParty(retention),

            // Retention regime
            new XElement(Sac + "SUNATRetentionSystemCode", retention.RegimeCode),
            new XElement(Sac + "SUNATRetentionPercent", Fmt(retention.RetentionPercent)),

            // Notes
            retention.Notes is not null
                ? new XElement(Cbc + "Note", retention.Notes)
                : null!,

            // Total amounts
            new XElement(Cbc + "TotalInvoiceAmount",
                new XAttribute("currencyID", retention.Currency),
                Fmt(retention.TotalInvoiceAmount)),
            new XElement(Sac + "SUNATTotalPaid",
                new XAttribute("currencyID", retention.Currency),
                Fmt(retention.TotalPaid)),
            new XElement(Sac + "SUNATTotalCashed",
                new XAttribute("currencyID", retention.Currency),
                Fmt(retention.TotalRetained)),

            // Document references (facturas asociadas)
            retention.References.OrderBy(r => r.PaymentDate).Select(BuildRetentionDocumentReference)
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
                    new XAttribute("schemeID", "6"), // 6=RUC
                    tenant.Ruc)),
            new XElement(Cac + "PartyName",
                new XElement(Cbc + "Name", new XCData(tenant.RazonSocial))),
            new XElement(Cac + "PartyLegalEntity",
                new XElement(Cbc + "RegistrationName",
                    new XCData(tenant.RazonSocial))));
    }

    private XElement BuildReceiverParty(RetentionDocument retention)
    {
        return new XElement(Cac + "ReceiverParty",
            new XElement(Cac + "PartyIdentification",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", retention.SupplierDocType),
                    retention.SupplierDocNumber)),
            new XElement(Cac + "PartyName",
                new XElement(Cbc + "Name", new XCData(retention.SupplierName))),
            new XElement(Cac + "PartyLegalEntity",
                new XElement(Cbc + "RegistrationName",
                    new XCData(retention.SupplierName))));
    }

    private XElement BuildRetentionDocumentReference(RetentionDocumentReference reference)
    {
        var refElement = new XElement(Sac + "SUNATRetentionDocumentReference",
            // Referenced document
            new XElement(Cbc + "ID",
                new XAttribute("schemeID", reference.DocumentType),
                reference.DocumentNumber),
            new XElement(Cbc + "IssueDate", reference.DocumentDate.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "TotalInvoiceAmount",
                new XAttribute("currencyID", reference.InvoiceCurrency),
                Fmt(reference.InvoiceAmount)),

            // Payment info
            new XElement(Cac + "Payment",
                new XElement(Cbc + "ID", reference.PaymentNumber),
                new XElement(Cbc + "PaidAmount",
                    new XAttribute("currencyID", reference.InvoiceCurrency),
                    Fmt(reference.PaymentAmount)),
                new XElement(Cbc + "PaidDate", reference.PaymentDate.ToString("yyyy-MM-dd"))),

            // Retention amounts
            new XElement(Sac + "SUNATRetentionAmount",
                new XAttribute("currencyID", "PEN"),
                Fmt(reference.RetainedAmount)),
            new XElement(Sac + "SUNATNetTotalPaid",
                new XAttribute("currencyID", "PEN"),
                Fmt(reference.NetPaidAmount))
        );

        // Exchange rate (if different currency)
        if (reference.ExchangeRate.HasValue && reference.InvoiceCurrency != "PEN")
        {
            refElement.Add(new XElement(Cac + "ExchangeRate",
                new XElement(Cbc + "SourceCurrencyCode", reference.InvoiceCurrency),
                new XElement(Cbc + "TargetCurrencyCode", "PEN"),
                new XElement(Cbc + "CalculationRate", Fmt4(reference.ExchangeRate.Value)),
                new XElement(Cbc + "Date", reference.ExchangeRateDate?.ToString("yyyy-MM-dd")
                    ?? reference.PaymentDate.ToString("yyyy-MM-dd"))));
        }

        return refElement;
    }

    private static string Fmt(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
    private static string Fmt4(decimal value) => value.ToString("F4", CultureInfo.InvariantCulture);
}
