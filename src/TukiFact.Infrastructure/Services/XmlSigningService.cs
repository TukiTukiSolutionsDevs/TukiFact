using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

public class XmlSigningService : IXmlSigningService
{
    public (string SignedXml, string DigestValue) SignXml(string xml, byte[] certificateData, string password)
    {
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        xmlDoc.LoadXml(xml);

        // Detect format: PEM (text) or PFX (binary)
        X509Certificate2 cert;
        if (password.StartsWith("PEM:"))
        {
            // PEM format stored as text bytes
            var pemText = Encoding.UTF8.GetString(certificateData);
            cert = X509Certificate2.CreateFromPem(pemText, pemText);
        }
        else
        {
            // PFX/P12 binary format
            cert = X509CertificateLoader.LoadPkcs12(certificateData, password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
        }

        var rsaKey = cert.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Certificate does not contain RSA private key");

        // Create signed XML
        var signedXml = new SignedXml(xmlDoc) { SigningKey = rsaKey };

        // Reference to sign the entire document
        var reference = new Reference { Uri = "" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        signedXml.AddReference(reference);

        // Key info with X509 data
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        // Compute signature
        signedXml.ComputeSignature();

        // Get the signature element
        var signatureElement = signedXml.GetXml();

        // Insert signature into UBLExtensions/UBLExtension/ExtensionContent
        var nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsMgr.AddNamespace("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");

        var extensionContent = xmlDoc.SelectSingleNode("//ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent", nsMgr);
        if (extensionContent != null)
        {
            extensionContent.AppendChild(xmlDoc.ImportNode(signatureElement, true));
        }

        // Get digest value for hash code
        var digestValue = Convert.ToBase64String(
            reference.DigestValue ?? []);

        return (xmlDoc.OuterXml, digestValue);
    }
}
