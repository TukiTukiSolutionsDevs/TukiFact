using System.Globalization;
using System.Xml.Linq;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Enums;

namespace TukiFact.Infrastructure.Services;

public class UblBuilder : IUblBuilder
{
    // UBL 2.1 Namespaces required by SUNAT
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";
    private static readonly XNamespace Invoice = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace CreditNote = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";
    private static readonly XNamespace DebitNote = "urn:oasis:names:specification:ubl:schema:xsd:DebitNote-2";

    public string BuildInvoiceXml(Document document, Tenant tenant)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            BuildInvoiceRoot(document, tenant));

        using var sw = new StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    public string BuildCreditNoteXml(Document document, Tenant tenant)
    {
        var root = new XElement(CreditNote + "CreditNote",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", Cac.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ext", Ext.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ds", Ds.NamespaceName),

            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "2.0"),
            new XElement(Cbc + "ID", document.FullNumber),
            new XElement(Cbc + "IssueDate", document.IssueDate.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueTime", "00:00:00"),

            new XElement(Cbc + "CreditNoteTypeCode",
                new XAttribute("listID", "0112"),
                new XAttribute("listAgencyName", "PE:SUNAT"),
                new XAttribute("listName", "Tipo de Nota de Crédito"),
                new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo09"),
                document.CreditNoteReason ?? "01"),

            document.Notes is not null
                ? new XElement(Cbc + "Note",
                    new XAttribute("languageLocaleID", "1000"),
                    document.Notes)
                : null!,

            new XElement(Cbc + "DocumentCurrencyCode",
                new XAttribute("listID", "ISO 4217 Alpha"),
                new XAttribute("listName", "Currency"),
                new XAttribute("listAgencyName", "United Nations Economic Commission for Europe"),
                document.Currency),

            new XElement(Cbc + "LineCountNumeric", document.Items.Count),

            // Billing reference (original document)
            document.ReferenceDocumentNumber is not null
                ? new XElement(Cac + "BillingReference",
                    new XElement(Cac + "InvoiceDocumentReference",
                        new XElement(Cbc + "ID", document.ReferenceDocumentNumber),
                        new XElement(Cbc + "DocumentTypeCode",
                            new XAttribute("listAgencyName", "PE:SUNAT"),
                            new XAttribute("listName", "Tipo de Documento"),
                            new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"),
                            document.ReferenceDocumentType ?? "01")))
                : null!,

            BuildSignatureReference(tenant),
            BuildSupplierParty(tenant),
            BuildCustomerParty(document),
            BuildTaxTotal(document),
            BuildLegalMonetaryTotal(document),

            document.Items.OrderBy(i => i.Sequence).Select(item =>
                BuildNoteLine(item, document.Currency, "CreditNoteLine"))
        );

        root.Descendants().Where(e => e.Value == null && !e.HasElements && !e.HasAttributes).Remove();

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        using var sw = new StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    public string BuildDebitNoteXml(Document document, Tenant tenant)
    {
        var root = new XElement(DebitNote + "DebitNote",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", Cac.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ext", Ext.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ds", Ds.NamespaceName),

            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "2.0"),
            new XElement(Cbc + "ID", document.FullNumber),
            new XElement(Cbc + "IssueDate", document.IssueDate.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueTime", "00:00:00"),

            new XElement(Cbc + "DebitNoteTypeCode",
                new XAttribute("listID", "0113"),
                new XAttribute("listAgencyName", "PE:SUNAT"),
                new XAttribute("listName", "Tipo de Nota de Débito"),
                new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo10"),
                document.DebitNoteReason ?? "01"),

            document.Notes is not null
                ? new XElement(Cbc + "Note",
                    new XAttribute("languageLocaleID", "1000"),
                    document.Notes)
                : null!,

            new XElement(Cbc + "DocumentCurrencyCode",
                new XAttribute("listID", "ISO 4217 Alpha"),
                new XAttribute("listName", "Currency"),
                new XAttribute("listAgencyName", "United Nations Economic Commission for Europe"),
                document.Currency),

            new XElement(Cbc + "LineCountNumeric", document.Items.Count),

            document.ReferenceDocumentNumber is not null
                ? new XElement(Cac + "BillingReference",
                    new XElement(Cac + "InvoiceDocumentReference",
                        new XElement(Cbc + "ID", document.ReferenceDocumentNumber),
                        new XElement(Cbc + "DocumentTypeCode",
                            new XAttribute("listAgencyName", "PE:SUNAT"),
                            new XAttribute("listName", "Tipo de Documento"),
                            new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"),
                            document.ReferenceDocumentType ?? "01")))
                : null!,

            BuildSignatureReference(tenant),
            BuildSupplierParty(tenant),
            BuildCustomerParty(document),
            BuildTaxTotal(document),
            BuildLegalMonetaryTotal(document),

            document.Items.OrderBy(i => i.Sequence).Select(item =>
                BuildNoteLine(item, document.Currency, "DebitNoteLine"))
        );

        root.Descendants().Where(e => e.Value == null && !e.HasElements && !e.HasAttributes).Remove();

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        using var sw = new StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    private XElement BuildNoteLine(DocumentItem item, string currency, string lineElementName)
    {
        var igvCode = IgvType.GetSunatCode(item.IgvType);
        var igvName = IgvType.GetSunatName(item.IgvType);
        var isGravado = item.IgvType == "10";

        return new XElement(Cac + lineElementName,
            new XElement(Cbc + "ID", item.Sequence),
            new XElement(Cbc + "CreditedQuantity",
                new XAttribute("unitCode", item.UnitMeasure),
                new XAttribute("unitCodeListID", "UN/ECE rec 20"),
                new XAttribute("unitCodeListAgencyName", "United Nations Economic Commission for Europe"),
                Fmt4(item.Quantity)),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", currency),
                Fmt(item.Subtotal)),
            new XElement(Cac + "PricingReference",
                new XElement(Cac + "AlternativeConditionPrice",
                    new XElement(Cbc + "PriceAmount",
                        new XAttribute("currencyID", currency),
                        Fmt4(item.UnitPriceWithIgv)),
                    new XElement(Cbc + "PriceTypeCode",
                        new XAttribute("listName", "Tipo de Precio"),
                        new XAttribute("listAgencyName", "PE:SUNAT"),
                        new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo16"),
                        "01"))),
            new XElement(Cac + "TaxTotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", currency),
                    Fmt(item.IgvAmount)),
                new XElement(Cac + "TaxSubtotal",
                    new XElement(Cbc + "TaxableAmount",
                        new XAttribute("currencyID", currency),
                        Fmt(item.Subtotal)),
                    new XElement(Cbc + "TaxAmount",
                        new XAttribute("currencyID", currency),
                        Fmt(item.IgvAmount)),
                    new XElement(Cac + "TaxCategory",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeID", "UN/ECE 5305"),
                            new XAttribute("schemeName", "Tax Category Identifier"),
                            new XAttribute("schemeAgencyName", "United Nations Economic Commission for Europe"),
                            isGravado ? "S" : "E"),
                        new XElement(Cbc + "Percent", isGravado ? "18.00" : "0.00"),
                        new XElement(Cbc + "TaxExemptionReasonCode",
                            new XAttribute("listAgencyName", "PE:SUNAT"),
                            new XAttribute("listName", "Afectacion del IGV"),
                            new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo07"),
                            item.IgvType),
                        new XElement(Cac + "TaxScheme",
                            new XElement(Cbc + "ID",
                                new XAttribute("schemeID", "UN/ECE 5153"),
                                new XAttribute("schemeAgencyID", "6"),
                                igvCode),
                            new XElement(Cbc + "Name", igvName),
                            new XElement(Cbc + "TaxTypeCode", isGravado ? "VAT" : "FRE"))))),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Description", new XCData(item.Description)),
                item.SunatProductCode is not null
                    ? new XElement(Cac + "CommodityClassification",
                        new XElement(Cbc + "ItemClassificationCode",
                            new XAttribute("listID", "UNSPSC"),
                            new XAttribute("listAgencyName", "GS1 US"),
                            new XAttribute("listName", "Item Classification"),
                            item.SunatProductCode))
                    : null!),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", currency),
                    Fmt4(item.UnitPrice))));
    }

    private XElement BuildInvoiceRoot(Document document, Tenant tenant)
    {
        var root = new XElement(Invoice + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", Cac.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ext", Ext.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ds", Ds.NamespaceName),

            // UBLExtensions (placeholder for digital signature)
            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            // UBL Version
            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "2.0"),

            // Document ID
            new XElement(Cbc + "ID", document.FullNumber),

            // Issue date/time
            new XElement(Cbc + "IssueDate", document.IssueDate.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueTime", "00:00:00"),

            // Due date
            document.DueDate.HasValue
                ? new XElement(Cbc + "DueDate", document.DueDate.Value.ToString("yyyy-MM-dd"))
                : null!,

            // Invoice type code
            new XElement(Cbc + "InvoiceTypeCode",
                new XAttribute("listID", "0101"),
                new XAttribute("listAgencyName", "PE:SUNAT"),
                new XAttribute("listName", "Tipo de Documento"),
                new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"),
                document.DocumentType),

            // Notes
            document.Notes is not null
                ? new XElement(Cbc + "Note",
                    new XAttribute("languageLocaleID", "1000"),
                    document.Notes)
                : null!,

            // Currency
            new XElement(Cbc + "DocumentCurrencyCode",
                new XAttribute("listID", "ISO 4217 Alpha"),
                new XAttribute("listName", "Currency"),
                new XAttribute("listAgencyName", "United Nations Economic Commission for Europe"),
                document.Currency),

            // Line count
            new XElement(Cbc + "LineCountNumeric", document.Items.Count),

            // Purchase order reference
            document.PurchaseOrder is not null
                ? new XElement(Cac + "OrderReference",
                    new XElement(Cbc + "ID", document.PurchaseOrder))
                : null!,

            // Signature reference
            BuildSignatureReference(tenant),

            // Supplier (emisor)
            BuildSupplierParty(tenant),

            // Customer (receptor)
            BuildCustomerParty(document),

            // Tax totals
            BuildTaxTotal(document),

            // Legal monetary total
            BuildLegalMonetaryTotal(document),

            // Invoice lines
            document.Items.OrderBy(i => i.Sequence).Select(item =>
                BuildInvoiceLine(item, document.Currency))
        );

        // Remove null elements
        root.Descendants().Where(e => e.Value == null && !e.HasElements && !e.HasAttributes).Remove();

        return root;
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

    private XElement BuildSupplierParty(Tenant tenant)
    {
        return new XElement(Cac + "AccountingSupplierParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", "6"),
                        new XAttribute("schemeName", "Documento de Identidad"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                        tenant.Ruc)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", new XCData(tenant.NombreComercial ?? tenant.RazonSocial))),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", new XCData(tenant.RazonSocial)),
                    new XElement(Cac + "RegistrationAddress",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeName", "Ubigeos"),
                            new XAttribute("schemeAgencyName", "PE:INEI"),
                            tenant.Ubigeo ?? "150101"),
                        new XElement(Cbc + "AddressTypeCode", "0000"),
                        new XElement(Cbc + "CityName", tenant.Provincia ?? "LIMA"),
                        new XElement(Cbc + "CountrySubentity", tenant.Departamento ?? "LIMA"),
                        new XElement(Cbc + "District", tenant.Distrito ?? "LIMA"),
                        new XElement(Cac + "AddressLine",
                            new XElement(Cbc + "Line", new XCData(tenant.Direccion ?? ""))),
                        new XElement(Cac + "Country",
                            new XElement(Cbc + "IdentificationCode", "PE"))))));
    }

    private XElement BuildCustomerParty(Document document)
    {
        return new XElement(Cac + "AccountingCustomerParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", document.CustomerDocType),
                        new XAttribute("schemeName", "Documento de Identidad"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                        document.CustomerDocNumber)),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", new XCData(document.CustomerName)),
                    document.CustomerAddress is not null
                        ? new XElement(Cac + "RegistrationAddress",
                            new XElement(Cac + "AddressLine",
                                new XElement(Cbc + "Line", new XCData(document.CustomerAddress))))
                        : null!)));
    }

    private XElement BuildTaxTotal(Document document)
    {
        var elements = new List<XElement>
        {
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", document.Currency),
                Fmt(document.Igv))
        };

        // IGV subtotal (gravado)
        if (document.OperacionGravada > 0)
        {
            elements.Add(BuildTaxSubtotal(document.OperacionGravada, document.Igv, document.Currency, "1000", "IGV", "VAT"));
        }

        // Exonerado
        if (document.OperacionExonerada > 0)
        {
            elements.Add(BuildTaxSubtotal(document.OperacionExonerada, 0, document.Currency, "9997", "EXO", "VAT"));
        }

        // Inafecto
        if (document.OperacionInafecta > 0)
        {
            elements.Add(BuildTaxSubtotal(document.OperacionInafecta, 0, document.Currency, "9998", "INA", "FRE"));
        }

        // Gratuito
        if (document.OperacionGratuita > 0)
        {
            elements.Add(BuildTaxSubtotal(document.OperacionGratuita, 0, document.Currency, "9996", "GRA", "FRE"));
        }

        return new XElement(Cac + "TaxTotal", elements);
    }

    private XElement BuildTaxSubtotal(decimal taxableAmount, decimal taxAmount, string currency, string taxCode, string taxName, string taxScheme)
    {
        return new XElement(Cac + "TaxSubtotal",
            new XElement(Cbc + "TaxableAmount",
                new XAttribute("currencyID", currency), Fmt(taxableAmount)),
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", currency), Fmt(taxAmount)),
            new XElement(Cac + "TaxCategory",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", "UN/ECE 5305"),
                    new XAttribute("schemeName", "Tax Category Identifier"),
                    new XAttribute("schemeAgencyName", "United Nations Economic Commission for Europe"),
                    taxScheme == "VAT" ? "S" : "E"),
                new XElement(Cac + "TaxScheme",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", "UN/ECE 5153"),
                        new XAttribute("schemeAgencyID", "6"),
                        taxCode),
                    new XElement(Cbc + "Name", taxName),
                    new XElement(Cbc + "TaxTypeCode", taxScheme))));
    }

    private XElement BuildLegalMonetaryTotal(Document document)
    {
        return new XElement(Cac + "LegalMonetaryTotal",
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", document.Currency),
                Fmt(document.OperacionGravada + document.OperacionExonerada + document.OperacionInafecta)),
            new XElement(Cbc + "TaxInclusiveAmount",
                new XAttribute("currencyID", document.Currency),
                Fmt(document.Total)),
            document.TotalDescuento > 0
                ? new XElement(Cbc + "AllowanceTotalAmount",
                    new XAttribute("currencyID", document.Currency),
                    Fmt(document.TotalDescuento))
                : null!,
            new XElement(Cbc + "PayableAmount",
                new XAttribute("currencyID", document.Currency),
                Fmt(document.Total)));
    }

    private XElement BuildInvoiceLine(DocumentItem item, string currency)
    {
        var igvCode = IgvType.GetSunatCode(item.IgvType);
        var igvName = IgvType.GetSunatName(item.IgvType);
        var isGravado = item.IgvType == "10";

        return new XElement(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", item.Sequence),
            new XElement(Cbc + "InvoicedQuantity",
                new XAttribute("unitCode", item.UnitMeasure),
                new XAttribute("unitCodeListID", "UN/ECE rec 20"),
                new XAttribute("unitCodeListAgencyName", "United Nations Economic Commission for Europe"),
                Fmt4(item.Quantity)),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", currency),
                Fmt(item.Subtotal)),
            new XElement(Cac + "PricingReference",
                new XElement(Cac + "AlternativeConditionPrice",
                    new XElement(Cbc + "PriceAmount",
                        new XAttribute("currencyID", currency),
                        Fmt4(item.UnitPriceWithIgv)),
                    new XElement(Cbc + "PriceTypeCode",
                        new XAttribute("listName", "Tipo de Precio"),
                        new XAttribute("listAgencyName", "PE:SUNAT"),
                        new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo16"),
                        "01"))),
            new XElement(Cac + "TaxTotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", currency),
                    Fmt(item.IgvAmount)),
                new XElement(Cac + "TaxSubtotal",
                    new XElement(Cbc + "TaxableAmount",
                        new XAttribute("currencyID", currency),
                        Fmt(item.Subtotal)),
                    new XElement(Cbc + "TaxAmount",
                        new XAttribute("currencyID", currency),
                        Fmt(item.IgvAmount)),
                    new XElement(Cac + "TaxCategory",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeID", "UN/ECE 5305"),
                            new XAttribute("schemeName", "Tax Category Identifier"),
                            new XAttribute("schemeAgencyName", "United Nations Economic Commission for Europe"),
                            isGravado ? "S" : "E"),
                        new XElement(Cbc + "Percent", isGravado ? "18.00" : "0.00"),
                        new XElement(Cbc + "TaxExemptionReasonCode",
                            new XAttribute("listAgencyName", "PE:SUNAT"),
                            new XAttribute("listName", "Afectacion del IGV"),
                            new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo07"),
                            item.IgvType),
                        new XElement(Cac + "TaxScheme",
                            new XElement(Cbc + "ID",
                                new XAttribute("schemeID", "UN/ECE 5153"),
                                new XAttribute("schemeAgencyID", "6"),
                                igvCode),
                            new XElement(Cbc + "Name", igvName),
                            new XElement(Cbc + "TaxTypeCode", isGravado ? "VAT" : "FRE"))))),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Description", new XCData(item.Description)),
                item.SunatProductCode is not null
                    ? new XElement(Cac + "CommodityClassification",
                        new XElement(Cbc + "ItemClassificationCode",
                            new XAttribute("listID", "UNSPSC"),
                            new XAttribute("listAgencyName", "GS1 US"),
                            new XAttribute("listName", "Item Classification"),
                            item.SunatProductCode))
                    : null!),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", currency),
                    Fmt4(item.UnitPrice))));
    }

    private static string Fmt(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
    private static string Fmt4(decimal value) => value.ToString("F4", CultureInfo.InvariantCulture);
}
