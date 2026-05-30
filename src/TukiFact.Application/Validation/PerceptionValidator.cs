using System.Text.RegularExpressions;
using TukiFact.Application.DTOs.Perceptions;

namespace TukiFact.Application.Validation;

/// <summary>
/// Hand-rolled validator for the perceptions emission flow (SUNAT type 40).
/// Per Reglamento SUNAT: perceptions are settled in PEN; agent has RUC; regime codes
/// {01,02,03} map fixed to percentages {2.0, 1.0, 0.5}.
/// </summary>
public static class PerceptionValidator
{
    private static readonly Regex SerieP = new("^P[A-Z0-9]{3}$", RegexOptions.Compiled);
    private static readonly Regex DocNumberShape = new("^[A-Z]\\d{3}-\\d{1,8}$", RegexOptions.Compiled);

    private static readonly Dictionary<string, decimal> RegimePercent = new()
    {
        ["01"] = 2.00m, // Venta interna
        ["02"] = 1.00m, // Combustibles
        ["03"] = 0.50m, // Importación de bienes (CdP)
    };

    public static List<string> Validate(CreatePerceptionRequest req)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(req.Serie) || !SerieP.IsMatch(req.Serie))
            errors.Add($"La serie '{req.Serie}' es inválida. Para Percepciones debe empezar con P (ej. P001).");

        if (!RegimePercent.TryGetValue(req.RegimeCode, out var expectedPct))
            errors.Add($"Régimen '{req.RegimeCode}' inválido. Usa 01 (Venta interna 2%), 02 (Combustible 1%) o 03 (CdP 0.5%).");
        else if (req.PerceptionPercent != expectedPct)
            errors.Add($"El porcentaje {req.PerceptionPercent} no coincide con el régimen {req.RegimeCode} (esperado {expectedPct}%).");

        // SUNAT permite USD pero la percepción se liquida y declara en PEN.
        if (req.Currency != "PEN")
            errors.Add("Las percepciones se liquidan en PEN. Cambia la moneda del comprobante a PEN.");

        // Customer = adquiriente (debe ser RUC en la mayoría de regímenes; toleramos otros tipos pero validamos formato).
        if (req.CustomerDocType != "6")
            errors.Add("El adquiriente debe estar identificado con RUC (tipo de documento 6).");
        else if (!SunatIdentity.IsValidRuc(req.CustomerDocNumber))
            errors.Add($"El RUC del adquiriente '{req.CustomerDocNumber}' es inválido.");

        if (string.IsNullOrWhiteSpace(req.CustomerName))
            errors.Add("La razón social del adquiriente es obligatoria.");

        if (req.References is null || req.References.Count == 0)
        {
            errors.Add("La percepción debe referenciar al menos un comprobante.");
        }
        else
        {
            if (req.References.Count > 100)
                errors.Add("Una sola percepción no puede referenciar más de 100 comprobantes.");

            for (int i = 0; i < req.References.Count; i++)
                ValidateReference(req.References[i], i + 1, errors);
        }

        if (!string.IsNullOrEmpty(req.Notes) && req.Notes.Length > 500)
            errors.Add("Las observaciones no deben exceder 500 caracteres.");

        return errors;
    }

    private static void ValidateReference(CreatePerceptionReferenceRequest r, int idx, List<string> errors)
    {
        var prefix = $"Referencia {idx}:";

        if (string.IsNullOrWhiteSpace(r.DocumentNumber) || !DocNumberShape.IsMatch(r.DocumentNumber))
            errors.Add($"{prefix} número '{r.DocumentNumber}' inválido. Debe ser SERIE-CORRELATIVO (ej. F001-1234).");

        if (r.InvoiceAmount <= 0) errors.Add($"{prefix} el monto del comprobante debe ser mayor a 0.");
        if (r.CollectionAmount <= 0) errors.Add($"{prefix} el monto cobrado debe ser mayor a 0.");
        if (r.CollectionAmount > r.InvoiceAmount + 0.01m)
            errors.Add($"{prefix} el cobro ({r.CollectionAmount}) no puede exceder el monto del comprobante ({r.InvoiceAmount}).");

        if (!SunatIdentity.ValidCurrencies.Contains(r.InvoiceCurrency))
            errors.Add($"{prefix} la moneda '{r.InvoiceCurrency}' no es válida.");

        if (r.InvoiceCurrency != "PEN")
        {
            if (r.ExchangeRate is null or <= 0)
                errors.Add($"{prefix} el tipo de cambio es obligatorio cuando la moneda del comprobante no es PEN.");
            if (r.ExchangeRateDate is null)
                errors.Add($"{prefix} la fecha del tipo de cambio es obligatoria cuando la moneda del comprobante no es PEN.");
        }
    }
}
