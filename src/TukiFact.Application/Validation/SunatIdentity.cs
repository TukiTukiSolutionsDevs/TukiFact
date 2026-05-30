namespace TukiFact.Application.Validation;

/// <summary>
/// Shared SUNAT identity / catálogo helpers used across hand-rolled validators.
/// Centralises the mod-11 RUC check and the regex shapes so each flow doesn't roll its own.
/// </summary>
internal static class SunatIdentity
{
    public static bool IsValidRuc(string? ruc)
    {
        if (string.IsNullOrEmpty(ruc) || ruc.Length != 11) return false;
        for (int i = 0; i < 11; i++) if (!char.IsDigit(ruc[i])) return false;
        if (ruc[0] != '1' && ruc[0] != '2') return false; // RUC starts in 10 (natural) or 20 (juridical)

        ReadOnlySpan<int> weights = stackalloc int[] { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
        var sum = 0;
        for (int i = 0; i < 10; i++) sum += (ruc[i] - '0') * weights[i];

        var rest = 11 - (sum % 11);
        var check = rest switch
        {
            10 => 0,
            11 => 1,
            _ => rest,
        };
        return check == (ruc[10] - '0');
    }

    public static bool IsValidDni(string? dni)
    {
        if (string.IsNullOrEmpty(dni) || dni.Length != 8) return false;
        for (int i = 0; i < 8; i++) if (!char.IsDigit(dni[i])) return false;
        return true;
    }

    /// <summary>Catálogo 06 (Tipo de documento de identidad del adquiriente).</summary>
    public static readonly HashSet<string> ValidCustomerDocTypes = new()
    {
        "0", // No domiciliado
        "1", // DNI
        "4", // Carnet de extranjería
        "6", // RUC
        "7", // Pasaporte
        "A", // Cédula diplomática
    };

    /// <summary>Catálogo 02 — moneda.</summary>
    public static readonly HashSet<string> ValidCurrencies = new() { "PEN", "USD" };

    /// <summary>Catálogo 07 — tipo de afectación IGV.</summary>
    public static readonly HashSet<string> ValidIgvTypes = new()
    {
        "10", "11", "12", "13", "14", "15", "16", "17", // gravado / retiro
        "20", "21",                                      // exonerado
        "30", "31", "32", "33", "34", "35", "36",        // inafecto
        "40",                                            // exportación
    };

    public static bool IsValidIdentity(string docType, string? docNumber)
    {
        if (string.IsNullOrEmpty(docNumber)) return false;
        return docType switch
        {
            "1" => IsValidDni(docNumber),
            "6" => IsValidRuc(docNumber),
            "0" or "4" or "7" or "A" => docNumber.Length is >= 1 and <= 15,
            _ => false,
        };
    }
}
