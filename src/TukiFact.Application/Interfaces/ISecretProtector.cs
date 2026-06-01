namespace TukiFact.Application.Interfaces;

/// <summary>
/// Encrypts/decrypts tenant secrets (certificate password, SOL password) at-rest.
/// Backed by ASP.NET Core DataProtection so the key ring is managed centrally.
///
/// Format: encrypted strings are prefixed with "enc:v1:" so that plain-text legacy values
/// can be detected and transparently re-encrypted on next save.
/// </summary>
public interface ISecretProtector
{
    /// <summary>Encrypt a plain-text secret. Returns "enc:v1:&lt;base64&gt;".</summary>
    string Protect(string? plain);

    /// <summary>
    /// Decrypt a possibly-encrypted secret. If the input has no enc:v1: prefix, returns it as-is
    /// (legacy plain-text values written before encryption was wired).
    /// </summary>
    string Unprotect(string? value);
}
