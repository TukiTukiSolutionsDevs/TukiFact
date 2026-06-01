using System.Text.RegularExpressions;
using TukiFact.Application.DTOs.RecurringInvoices;

namespace TukiFact.Application.Validation;

/// <summary>
/// Hand-rolled validator for recurring invoice create requests. Mirrors
/// DespatchAdviceValidator — accumulates every violation and returns the full list
/// so the user fixes everything at once instead of one error at a time.
/// </summary>
public static class RecurringInvoiceValidator
{
    private static readonly HashSet<string> ValidDocumentTypes = new() { "01", "03" };
    private static readonly HashSet<string> ValidCustomerDocTypes = new() { "0", "1", "4", "6", "7" };
    private static readonly HashSet<string> ValidCurrencies = new() { "PEN", "USD" };
    private static readonly HashSet<string> ValidFrequencies = new()
    {
        "daily", "weekly", "biweekly", "monthly", "yearly",
    };
    private static readonly HashSet<string> ValidIgvTypes = new() { "10", "20", "30" };

    private static readonly Regex SerieFacturaRe = new("^F\\d{3}$", RegexOptions.Compiled);
    private static readonly Regex SerieBoletaRe = new("^B\\d{3}$", RegexOptions.Compiled);
    private static readonly Regex DigitsOnlyRe = new("^\\d+$", RegexOptions.Compiled);
    private static readonly Regex EmailRe = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled);

    public static List<string> Validate(CreateRecurringInvoiceRequest req)
    {
        var errors = new List<string>();

        // ---- Document type & serie ----
        if (!ValidDocumentTypes.Contains(req.DocumentType))
            errors.Add($"Tipo de comprobante '{req.DocumentType}' inválido. Debe ser 01 (Factura) o 03 (Boleta).");

        if (string.IsNullOrWhiteSpace(req.Serie))
            errors.Add("La serie es obligatoria.");
        else if (req.DocumentType == "01" && !SerieFacturaRe.IsMatch(req.Serie))
            errors.Add($"Para facturas la serie debe empezar con F (ej. F001). Recibido: {req.Serie}");
        else if (req.DocumentType == "03" && !SerieBoletaRe.IsMatch(req.Serie))
            errors.Add($"Para boletas la serie debe empezar con B (ej. B001). Recibido: {req.Serie}");

        // ---- Customer ----
        if (!ValidCustomerDocTypes.Contains(req.CustomerDocType))
            errors.Add($"Tipo de documento del cliente '{req.CustomerDocType}' inválido.");
        else
        {
            switch (req.CustomerDocType)
            {
                case "6":
                    if (string.IsNullOrWhiteSpace(req.CustomerDocNumber) ||
                        req.CustomerDocNumber.Length != 11 ||
                        !DigitsOnlyRe.IsMatch(req.CustomerDocNumber))
                        errors.Add("El RUC del cliente debe tener 11 dígitos.");
                    break;
                case "1":
                    if (string.IsNullOrWhiteSpace(req.CustomerDocNumber) ||
                        req.CustomerDocNumber.Length != 8 ||
                        !DigitsOnlyRe.IsMatch(req.CustomerDocNumber))
                        errors.Add("El DNI del cliente debe tener 8 dígitos.");
                    break;
                case "0":
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(req.CustomerDocNumber))
                        errors.Add("El número de documento del cliente es obligatorio.");
                    break;
            }
        }

        if (req.DocumentType == "01" && req.CustomerDocType != "6")
            errors.Add("Para emitir factura el cliente debe tener RUC (tipo doc. 6).");

        if (string.IsNullOrWhiteSpace(req.CustomerName))
            errors.Add("El nombre o razón social del cliente es obligatorio.");
        else if (req.CustomerName.Length > 200)
            errors.Add("El nombre del cliente no debe exceder 200 caracteres.");

        if (!string.IsNullOrEmpty(req.CustomerAddress) && req.CustomerAddress.Length > 300)
            errors.Add("La dirección del cliente no debe exceder 300 caracteres.");

        if (!string.IsNullOrEmpty(req.CustomerEmail))
        {
            if (req.CustomerEmail.Length > 200)
                errors.Add("El email del cliente no debe exceder 200 caracteres.");
            else if (!EmailRe.IsMatch(req.CustomerEmail))
                errors.Add($"El email del cliente '{req.CustomerEmail}' no es válido.");
        }

        // ---- Currency ----
        if (!string.IsNullOrEmpty(req.Currency) && !ValidCurrencies.Contains(req.Currency))
            errors.Add($"Moneda '{req.Currency}' no soportada. Usa PEN o USD.");

        // ---- Frequency ----
        if (!ValidFrequencies.Contains(req.Frequency))
            errors.Add($"Frecuencia '{req.Frequency}' inválida. Usa daily, weekly, biweekly, monthly o yearly.");
        else if (req.Frequency == "monthly")
        {
            if (!req.DayOfMonth.HasValue)
                errors.Add("Para frecuencia mensual debes indicar el día del mes (1–28).");
            else if (req.DayOfMonth.Value < 1 || req.DayOfMonth.Value > 28)
                errors.Add($"Día del mes {req.DayOfMonth.Value} fuera de rango. Usa 1–28 para garantizar que cae todos los meses.");
        }
        else if (req.Frequency == "weekly")
        {
            if (!req.DayOfWeek.HasValue)
                errors.Add("Para frecuencia semanal debes indicar el día de la semana (0=Domingo, 6=Sábado).");
            else if (req.DayOfWeek.Value < 0 || req.DayOfWeek.Value > 6)
                errors.Add($"Día de la semana {req.DayOfWeek.Value} fuera de rango. Usa 0–6.");
        }

        // ---- Dates ----
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (req.StartDate < today)
            errors.Add($"La fecha de inicio ({req.StartDate:yyyy-MM-dd}) no puede ser anterior a hoy.");

        if (req.EndDate.HasValue && req.EndDate.Value < req.StartDate)
            errors.Add($"La fecha de fin ({req.EndDate.Value:yyyy-MM-dd}) no puede ser anterior a la fecha de inicio ({req.StartDate:yyyy-MM-dd}).");

        // ---- Notes ----
        if (!string.IsNullOrEmpty(req.Notes) && req.Notes.Length > 500)
            errors.Add("Las notas no deben exceder 500 caracteres.");

        // ---- Items ----
        if (req.Items is null || req.Items.Count == 0)
            errors.Add("Debe incluir al menos un item en la plantilla.");
        else
        {
            for (var i = 0; i < req.Items.Count; i++)
            {
                var item = req.Items[i];
                var prefix = $"Item {i + 1}";

                if (string.IsNullOrWhiteSpace(item.Description))
                    errors.Add($"{prefix}: la descripción es obligatoria.");
                else if (item.Description.Length > 250)
                    errors.Add($"{prefix}: la descripción no debe exceder 250 caracteres.");

                if (item.Quantity <= 0)
                    errors.Add($"{prefix}: la cantidad debe ser mayor a 0.");

                if (item.UnitPrice < 0)
                    errors.Add($"{prefix}: el precio unitario no puede ser negativo.");

                if (!string.IsNullOrEmpty(item.IgvType) && !ValidIgvTypes.Contains(item.IgvType))
                    errors.Add($"{prefix}: tipo de IGV '{item.IgvType}' inválido. Usa 10 (Gravado), 20 (Exonerado) o 30 (Inafecto).");
            }
        }

        return errors;
    }
}
