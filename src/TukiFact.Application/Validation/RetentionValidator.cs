using System.Text.RegularExpressions;
using TukiFact.Application.DTOs.Retentions;

namespace TukiFact.Application.Validation;

/// <summary>
/// Hand-rolled validator for retentions emission flow (SUNAT type 20).
/// Régimen 01 = Tasa 3%, Régimen 02 = Tasa 6%.
/// </summary>
public static class RetentionValidator
{
    private static readonly Regex SerieR = new("^R[A-Z0-9]{3}$", RegexOptions.Compiled);
    private static readonly Regex DocNumberShape = new("^[A-Z]\\d{3}-\\d{1,8}$", RegexOptions.Compiled);

    private static readonly Dictionary<string, decimal> RegimePercent = new()
    {
        ["01"] = 3.00m,
        ["02"] = 6.00m,
    };

    public static List<string> Validate(CreateRetentionRequest req)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(req.Serie) || !SerieR.IsMatch(req.Serie))
            errors.Add($"La serie '{req.Serie}' es inválida. Para Retenciones debe empezar con R (ej. R001).");

        if (!RegimePercent.TryGetValue(req.RegimeCode, out var expectedPct))
            errors.Add($"Régimen '{req.RegimeCode}' inválido. Usa 01 (Tasa 3%) o 02 (Tasa 6%).");
        else if (req.RetentionPercent != expectedPct)
            errors.Add($"El porcentaje {req.RetentionPercent} no coincide con el régimen {req.RegimeCode} (esperado {expectedPct}%).");

        if (req.Currency != "PEN")
            errors.Add("Las retenciones se liquidan en PEN. Cambia la moneda del comprobante a PEN.");

        // Supplier = proveedor, debe ser RUC para retenciones.
        if (req.SupplierDocType != "6")
            errors.Add("El proveedor debe estar identificado con RUC (tipo de documento 6).");
        else if (!SunatIdentity.IsValidRuc(req.SupplierDocNumber))
            errors.Add($"El RUC del proveedor '{req.SupplierDocNumber}' es inválido.");

        if (string.IsNullOrWhiteSpace(req.SupplierName))
            errors.Add("La razón social del proveedor es obligatoria.");

        if (req.References is null || req.References.Count == 0)
        {
            errors.Add("La retención debe referenciar al menos un comprobante.");
        }
        else
        {
            if (req.References.Count > 100)
                errors.Add("Una sola retención no puede referenciar más de 100 comprobantes.");

            for (int i = 0; i < req.References.Count; i++)
                ValidateReference(req.References[i], i + 1, errors);
        }

        if (!string.IsNullOrEmpty(req.Notes) && req.Notes.Length > 500)
            errors.Add("Las observaciones no deben exceder 500 caracteres.");

        return errors;
    }

    private static void ValidateReference(CreateRetentionReferenceRequest r, int idx, List<string> errors)
    {
        var prefix = $"Referencia {idx}:";

        if (string.IsNullOrWhiteSpace(r.DocumentNumber) || !DocNumberShape.IsMatch(r.DocumentNumber))
            errors.Add($"{prefix} número '{r.DocumentNumber}' inválido. Debe ser SERIE-CORRELATIVO (ej. F001-1234).");

        if (r.InvoiceAmount <= 0) errors.Add($"{prefix} el monto del comprobante debe ser mayor a 0.");
        if (r.PaymentAmount <= 0) errors.Add($"{prefix} el monto pagado debe ser mayor a 0.");
        if (r.PaymentAmount > r.InvoiceAmount + 0.01m)
            errors.Add($"{prefix} el pago ({r.PaymentAmount}) no puede exceder el monto del comprobante ({r.InvoiceAmount}).");

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
