using System.Globalization;
using System.Xml.Linq;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Builds DespatchAdvice UBL 2.1 XML for SUNAT GRE (Guía de Remisión Electrónica).
/// Reference: SUNAT Manual de servicios web GRE, R.S. 123-2022, R.S. 304-2024.
/// </summary>
public class GreXmlBuilder : IGreXmlBuilder
{
    private static readonly XNamespace DespatchNs = "urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";

    public string BuildDespatchAdviceXml(DespatchAdvice da, Tenant tenant)
    {
        var root = new XElement(DespatchNs + "DespatchAdvice",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", Cac.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ext", Ext.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ds", Ds.NamespaceName),

            // UBL Extensions (placeholder for digital signature)
            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "2.0"),

            // Document ID: T001-00000001
            new XElement(Cbc + "ID", da.FullNumber),

            // Issue date and time
            new XElement(Cbc + "IssueDate", da.IssueDate.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueTime", da.IssueTime.ToString("HH:mm:ss")),

            // Document type: 09=GRE Remitente, 31=GRE Transportista
            new XElement(Cbc + "DespatchAdviceTypeCode", da.DocumentType),

            // Notes
            da.Note is not null
                ? new XElement(Cbc + "Note", da.Note)
                : null!,

            // Related document reference (factura, boleta, etc.)
            da.RelatedDocNumber is not null
                ? new XElement(Cac + "OrderReference",
                    new XElement(Cbc + "ID", da.RelatedDocNumber),
                    new XElement(Cbc + "OrderTypeCode", da.RelatedDocType ?? "01"))
                : null!,

            // Additional document reference (for additional docs)
            BuildAdditionalDocumentReference(da),

            // Signature reference
            BuildSignatureReference(tenant),

            // Supplier party (remitente/emisor)
            BuildDespatchSupplierParty(da, tenant),

            // Customer party (destinatario)
            BuildDeliveryCustomerParty(da),

            // Seller supplier party (vendedor, if different from remitente)
            // Omitted for simplicity — same as supplier for GRE Remitente

            // Shipment
            BuildShipment(da),

            // Despatch lines (items)
            da.Items.OrderBy(i => i.LineNumber).Select(BuildDespatchLine)
        );

        // Remove null elements
        root.Descendants().Where(e => e.Value == null && !e.HasElements && !e.HasAttributes).Remove();

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        using var sw = new StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    private static XElement? BuildAdditionalDocumentReference(DespatchAdvice da)
    {
        // For GRE Remitente, no additional document reference needed by default
        return null;
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

    private XElement BuildDespatchSupplierParty(DespatchAdvice da, Tenant tenant)
    {
        return new XElement(Cac + "DespatchSupplierParty",
            new XElement(Cbc + "CustomerAssignedAccountID",
                new XAttribute("schemeID", "6"), // 6=RUC
                tenant.Ruc),
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName",
                        new XCData(tenant.RazonSocial)))));
    }

    private XElement BuildDeliveryCustomerParty(DespatchAdvice da)
    {
        return new XElement(Cac + "DeliveryCustomerParty",
            new XElement(Cbc + "CustomerAssignedAccountID",
                new XAttribute("schemeID", da.RecipientDocType),
                da.RecipientDocNumber),
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName",
                        new XCData(da.RecipientName)))));
    }

    private XElement BuildShipment(DespatchAdvice da)
    {
        var shipment = new XElement(Cac + "Shipment",
            new XElement(Cbc + "ID", "1"),

            // Transfer reason (Catálogo 20)
            new XElement(Cbc + "HandlingCode", da.TransferReasonCode),
            new XElement(Cbc + "HandlingInstructions", da.TransferReasonDescription),

            // Gross weight
            new XElement(Cbc + "GrossWeightMeasure",
                new XAttribute("unitCode", da.WeightUnitCode),
                Fmt(da.GrossWeight)),

            // Total packages
            new XElement(Cbc + "TotalTransportHandlingUnitQuantity", da.TotalPackages),

            // Shipment stage (transport mode + carrier/driver)
            BuildShipmentStage(da),

            // Delivery address (destination)
            new XElement(Cac + "DeliveryAddress",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeName", "Ubigeos"),
                    new XAttribute("schemeAgencyName", "PE:INEI"),
                    da.DestinationUbigeo),
                new XElement(Cac + "AddressLine",
                    new XElement(Cbc + "Line", da.DestinationAddress))),

            // Origin address
            new XElement(Cac + "OriginAddress",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeName", "Ubigeos"),
                    new XAttribute("schemeAgencyName", "PE:INEI"),
                    da.OriginUbigeo),
                new XElement(Cac + "AddressLine",
                    new XElement(Cbc + "Line", da.OriginAddress)))
        );

        return shipment;
    }

    private XElement BuildShipmentStage(DespatchAdvice da)
    {
        var stage = new XElement(Cac + "ShipmentStage",
            // Transport mode: 01=Público, 02=Privado
            new XElement(Cbc + "TransportModeCode", da.TransportMode),

            // Transit period
            new XElement(Cac + "TransitPeriod",
                new XElement(Cbc + "StartDate", da.TransferStartDate.ToString("yyyy-MM-dd")))
        );

        // Carrier party (transporte público)
        if (da.TransportMode == "01" && da.CarrierDocNumber is not null)
        {
            stage.Add(new XElement(Cac + "CarrierParty",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", da.CarrierDocType ?? "6"),
                        da.CarrierDocNumber)),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName",
                        new XCData(da.CarrierName ?? "")))));
        }

        // Driver (transporte privado)
        if (da.TransportMode == "02" && da.DriverDocNumber is not null)
        {
            stage.Add(new XElement(Cac + "DriverPerson",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", da.DriverDocType ?? "1"),
                    da.DriverDocNumber),
                new XElement(Cbc + "FirstName", da.DriverName ?? ""),
                da.DriverLicense is not null
                    ? new XElement(Cbc + "JobTitle", da.DriverLicense)
                    : null!));
        }

        // Vehicle (transporte privado)
        if (da.TransportMode == "02" && da.VehiclePlate is not null)
        {
            stage.Add(new XElement(Cac + "TransportMeans",
                new XElement(Cac + "RoadTransport",
                    new XElement(Cbc + "LicensePlateID", da.VehiclePlate))));

            if (da.SecondaryVehiclePlate is not null)
            {
                stage.Add(new XElement(Cac + "TransportMeans",
                    new XElement(Cac + "RoadTransport",
                        new XElement(Cbc + "LicensePlateID", da.SecondaryVehiclePlate))));
            }
        }

        // Remove null elements
        stage.Descendants().Where(e => e.Value == null && !e.HasElements && !e.HasAttributes).Remove();

        return stage;
    }

    private XElement BuildDespatchLine(DespatchAdviceItem item)
    {
        return new XElement(Cac + "DespatchLine",
            new XElement(Cbc + "ID", item.LineNumber),
            new XElement(Cbc + "DeliveredQuantity",
                new XAttribute("unitCode", item.UnitCode),
                Fmt4(item.Quantity)),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Name", new XCData(item.Description)),
                item.ProductCode is not null
                    ? new XElement(Cac + "SellersItemIdentification",
                        new XElement(Cbc + "ID", item.ProductCode))
                    : null!));
    }

    private static string Fmt(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
    private static string Fmt4(decimal value) => value.ToString("F4", CultureInfo.InvariantCulture);
}
