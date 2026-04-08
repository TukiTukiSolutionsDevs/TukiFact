using System.IO.Compression;
using System.Xml.Linq;

namespace TukiFact.Infrastructure.Services;

public static class CdrParser
{
    public static (string ResponseCode, string Description, List<string> Notes) ParseCdr(byte[] cdrZip)
    {
        using var ms = new MemoryStream(cdrZip);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

        var xmlEntry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("CDR ZIP does not contain an XML file");

        using var entryStream = xmlEntry.Open();
        var doc = XDocument.Load(entryStream);

        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        // Extract response code
        var responseCode = doc.Descendants(cbc + "ResponseCode").FirstOrDefault()?.Value ?? "unknown";

        // Extract description
        var description = doc.Descendants(cbc + "Description").FirstOrDefault()?.Value ?? "";

        // Extract notes (observations)
        var notes = doc.Descendants(cbc + "Note")
            .Select(n => n.Value)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        return (responseCode, description, notes);
    }
}
