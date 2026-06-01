using System.Text;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// StringWriter that reports UTF-8 instead of the default UTF-16, so that
/// XDocument.Save(...) emits an XML declaration of encoding="utf-8" matching
/// the actual bytes consumed downstream. SUNAT production may reject XML where
/// the declaration disagrees with the byte encoding (we always emit UTF-8 bytes
/// later via Encoding.UTF8.GetBytes); UTF-16 in the declaration is the bug.
/// </summary>
internal sealed class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}
