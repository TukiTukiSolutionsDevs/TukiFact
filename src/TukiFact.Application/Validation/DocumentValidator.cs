using System.Text.RegularExpressions;
using TukiFact.Application.DTOs.Documents;

namespace TukiFact.Application.Validation;

/// <summary>
/// Hand-rolled validator for the manual document emission flow (Factura, Boleta,
/// Nota de Crédito, Nota de Débito). Mirrors the GRE / Recurring pattern: returns
/// all violations in one list so the user fixes everything in a single round-trip.
/// </summary>
public static class DocumentValidator
{
    private const int MaxItems = 5000; // SUNAT hard limit is 30,000 but a tenant-side cap
    private const int MaxDescriptionLen = 250;
    private const int MaxItemsRows = 1000;

    private static readonly HashSet<string> ValidDocumentTypes = new() { "01", "03", "07", "08" };

    // Catálogo 09 SUNAT — motivos válidos de Nota de Crédito.
    private static readonly HashSet<string> ValidCreditNoteReasons = new()
    {
        "01", "02", "03", "04", "05", "06", "07", "08", "09", "10"
    };

    // Catálogo 10 SUNAT — motivos válidos de Nota de Débito.
    private static readonly HashSet<string> ValidDebitNoteReasons = new() { "01", "02", "03" };

    // Series shape per type (Catálogo 01).
    private static readonly Regex SerieFactura = new("^F[A-Z0-9]{3}$", RegexOptions.Compiled);
    private static readonly Regex SerieBoleta = new("^B[A-Z0-9]{3}$", RegexOptions.Compiled);
    // NC/ND inherit the prefix of the doc they reference (F or B).
    private static readonly Regex SerieNote = new("^[FB][A-Z0-9]{3}$", RegexOptions.Compiled);

    public static List<string> Validate(CreateDocumentRequest req)
    {
        var errors = new List<string>();

        if (!ValidDocumentTypes.Contains(req.DocumentType))
            errors.Add($"Tipo de documento '{req.DocumentType}' inválido. Usa 01 (Factura), 03 (Boleta), 07 (Nota de Crédito) o 08 (Nota de Débito).");

        ValidateSerie(req.DocumentType, req.Serie, errors);

        // Currency
        if (!SunatIdentity.ValidCurrencies.Contains(req.Currency))
            errors.Add($"Moneda '{req.Currency}' no soportada. Usa PEN o USD.");

        // Customer
        ValidateCustomer(req.DocumentType, req.CustomerDocType, req.CustomerDocNumber, req.CustomerName, req.CustomerEmail, errors);

        // Items
        ValidateItems(req.Items, errors);

        // Dates
        if (req.IssueDate.HasValue && req.DueDate.HasValue && req.DueDate.Value < req.IssueDate.Value)
            errors.Add("La fecha de vencimiento no puede ser anterior a la fecha de emisión.");

        // Notes
        if (req.Notes is { Length: > 500 }) errors.Add("Las observaciones no deben exceder 500 caracteres.");
        if (req.PurchaseOrder is { Length: > 30 }) errors.Add("La orden de compra no debe exceder 30 caracteres.");

        return errors;
    }

    public static List<string> ValidateCreditNote(CreateCreditNoteRequest req)
    {
        var errors = new List<string>();

        if (!SerieNote.IsMatch(req.Serie ?? string.Empty))
            errors.Add($"La serie '{req.Serie}' es inválida. Debe coincidir con la serie del comprobante de referencia (ej. F001 / B001).");

        if (req.ReferenceDocumentId == Guid.Empty)
            errors.Add("La nota de crédito requiere un documento de referencia.");

        if (string.IsNullOrWhiteSpace(req.CreditNoteReason))
            errors.Add("El código de motivo (Catálogo SUNAT 09) es obligatorio.");
        else if (!ValidCreditNoteReasons.Contains(req.CreditNoteReason))
            errors.Add($"El motivo '{req.CreditNoteReason}' no pertenece al Catálogo 09 SUNAT (válidos: 01–10).");

        if (string.IsNullOrWhiteSpace(req.Description) || req.Description.Trim().Length < 3)
            errors.Add("El sustento o descripción del motivo es obligatorio y debe tener al menos 3 caracteres (SUNAT lo exige en el UBL).");
        else if (req.Description.Length > 500)
            errors.Add("El sustento no debe exceder 500 caracteres.");

        if (!SunatIdentity.ValidCurrencies.Contains(req.Currency))
            errors.Add($"Moneda '{req.Currency}' no soportada. Usa PEN o USD.");

        ValidateItems(req.Items, errors);

        return errors;
    }

    public static List<string> ValidateDebitNote(CreateDebitNoteRequest req)
    {
        var errors = new List<string>();

        if (!SerieNote.IsMatch(req.Serie ?? string.Empty))
            errors.Add($"La serie '{req.Serie}' es inválida. Debe coincidir con la serie del comprobante de referencia (ej. F001 / B001).");

        if (req.ReferenceDocumentId == Guid.Empty)
            errors.Add("La nota de débito requiere un documento de referencia.");

        if (string.IsNullOrWhiteSpace(req.DebitNoteReason))
            errors.Add("El código de motivo (Catálogo SUNAT 10) es obligatorio.");
        else if (!ValidDebitNoteReasons.Contains(req.DebitNoteReason))
            errors.Add($"El motivo '{req.DebitNoteReason}' no pertenece al Catálogo 10 SUNAT (válidos: 01–03).");

        if (!SunatIdentity.ValidCurrencies.Contains(req.Currency))
            errors.Add($"Moneda '{req.Currency}' no soportada. Usa PEN o USD.");

        ValidateItems(req.Items, errors);

        return errors;
    }

    private static void ValidateSerie(string docType, string? serie, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(serie))
        {
            errors.Add("La serie es obligatoria.");
            return;
        }

        var ok = docType switch
        {
            "01" => SerieFactura.IsMatch(serie),
            "03" => SerieBoleta.IsMatch(serie),
            "07" or "08" => SerieNote.IsMatch(serie),
            _ => true,
        };

        if (!ok)
            errors.Add($"La serie '{serie}' no es válida para el tipo {docType}. Factura: F### · Boleta: B### · Notas: igual prefijo que el documento original.");
    }

    private static void ValidateCustomer(string docType, string customerDocType, string? customerDocNumber, string? customerName, string? customerEmail, List<string> errors)
    {
        if (!SunatIdentity.ValidCustomerDocTypes.Contains(customerDocType))
            errors.Add($"Tipo de documento del cliente '{customerDocType}' inválido (Catálogo 06).");

        if (string.IsNullOrWhiteSpace(customerDocNumber))
            errors.Add("El número de documento del cliente es obligatorio.");
        else if (!SunatIdentity.IsValidIdentity(customerDocType, customerDocNumber))
            errors.Add(customerDocType switch
            {
                "1" => $"El DNI '{customerDocNumber}' debe tener 8 dígitos numéricos.",
                "6" => $"El RUC '{customerDocNumber}' es inválido (11 dígitos + dígito verificador mod-11).",
                _ => $"El documento '{customerDocNumber}' no cumple el formato del tipo {customerDocType}.",
            });

        if (string.IsNullOrWhiteSpace(customerName))
            errors.Add("El nombre o razón social del cliente es obligatorio.");
        else if (customerName.Length > 200)
            errors.Add("El nombre del cliente no debe exceder 200 caracteres.");

        // Facturas (01) requieren cliente con RUC.
        if (docType == "01" && customerDocType != "6")
            errors.Add("Las facturas (tipo 01) requieren cliente con RUC (tipo de documento 6).");

        if (!string.IsNullOrEmpty(customerEmail) && !customerEmail.Contains('@'))
            errors.Add("El correo del cliente no tiene un formato válido.");
    }

    private static void ValidateItems(List<CreateDocumentItemRequest>? items, List<string> errors)
    {
        if (items is null || items.Count == 0)
        {
            errors.Add("El documento debe contener al menos un item.");
            return;
        }
        if (items.Count > MaxItems)
            errors.Add($"El documento no puede tener más de {MaxItems} items.");

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var prefix = $"Item {i + 1}:";

            if (string.IsNullOrWhiteSpace(item.Description))
                errors.Add($"{prefix} la descripción es obligatoria.");
            else if (item.Description.Length > MaxDescriptionLen)
                errors.Add($"{prefix} la descripción no debe exceder {MaxDescriptionLen} caracteres.");

            if (item.Quantity <= 0) errors.Add($"{prefix} la cantidad debe ser mayor a 0.");
            if (item.UnitPrice < 0) errors.Add($"{prefix} el precio unitario no puede ser negativo.");
            if (item.Discount < 0) errors.Add($"{prefix} el descuento no puede ser negativo.");
            if (string.IsNullOrWhiteSpace(item.UnitMeasure))
                errors.Add($"{prefix} la unidad de medida es obligatoria (ej. NIU, KGM, ZZ).");

            if (string.IsNullOrWhiteSpace(item.IgvType) || !SunatIdentity.ValidIgvTypes.Contains(item.IgvType))
                errors.Add($"{prefix} tipo de afectación IGV '{item.IgvType}' inválido (Catálogo 07).");
        }
    }
}
