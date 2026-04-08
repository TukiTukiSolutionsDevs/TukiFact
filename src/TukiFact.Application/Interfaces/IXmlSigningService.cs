namespace TukiFact.Application.Interfaces;

public interface IXmlSigningService
{
    (string SignedXml, string DigestValue) SignXml(string xml, byte[] certificateData, string password);
}
