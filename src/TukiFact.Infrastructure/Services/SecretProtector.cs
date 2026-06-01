using Microsoft.AspNetCore.DataProtection;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <inheritdoc cref="ISecretProtector" />
public class SecretProtector : ISecretProtector
{
    private const string Purpose = "TukiFact.SunatCreds.v1";
    private const string EncryptedPrefix = "enc:v1:";

    private readonly IDataProtector _protector;

    public SecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return string.Empty;
        // Already encrypted? Don't double-wrap.
        if (plain.StartsWith(EncryptedPrefix, StringComparison.Ordinal)) return plain;
        return EncryptedPrefix + _protector.Protect(plain);
    }

    public string Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (!value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            // Legacy plain-text — return as-is. Next save will re-encrypt.
            return value;
        }
        return _protector.Unprotect(value[EncryptedPrefix.Length..]);
    }
}
