using System.Text.RegularExpressions;
using TukiFact.Application.DTOs.DespatchAdvices;

namespace TukiFact.Application.Validation;

/// <summary>
/// Hand-rolled validator for GRE create requests. Aggregates all violations into a single
/// thrown exception so the user fixes everything at once (rather than fix→retry→next-error→retry).
/// </summary>
public static class DespatchAdviceValidator
{
    private static readonly HashSet<string> ValidDocumentTypes = new() { "09", "31" };
    private static readonly HashSet<string> ValidTransportModes = new() { "01", "02" };
    private static readonly HashSet<string> ValidRecipientDocTypes = new() { "0", "1", "4", "6", "7" };
    private static readonly HashSet<string> ValidCarrierDocTypes = new() { "1", "4", "6", "7" };
    private static readonly HashSet<string> ValidWeightUnits = new() { "KGM", "TNE", "GRM" };
    private static readonly HashSet<string> ValidTransferReasons = new()
    {
        "01", "02", "04", "08", "09", "13", "14", "18",
    };

    private static readonly Regex SerieRemitenteRe = new("^T\\d{3}$", RegexOptions.Compiled);
    private static readonly Regex SerieTransportistaRe = new("^V\\d{3}$", RegexOptions.Compiled);
    private static readonly Regex UbigeoRe = new("^\\d{6}$", RegexOptions.Compiled);
    private static readonly Regex DigitsOnlyRe = new("^\\d+$", RegexOptions.Compiled);
    private static readonly Regex PlateRe = new("^[A-Z0-9]{3}-?[A-Z0-9]{3,4}$", RegexOptions.Compiled);

    public static List<string> Validate(CreateDespatchAdviceRequest req)
    {
        var errors = new List<string>();

        // ---- Document type ----
        if (!ValidDocumentTypes.Contains(req.DocumentType))
            errors.Add($"Tipo de documento '{req.DocumentType}' inválido. Debe ser 09 (Remitente) o 31 (Transportista).");

        // ---- Serie ----
        if (string.IsNullOrWhiteSpace(req.Serie))
            errors.Add("La serie es obligatoria.");
        else if (req.DocumentType == "09" && !SerieRemitenteRe.IsMatch(req.Serie))
            errors.Add($"Para GRE Remitente la serie debe empezar con T (ej. T001). Recibido: {req.Serie}");
        else if (req.DocumentType == "31" && !SerieTransportistaRe.IsMatch(req.Serie))
            errors.Add($"Para GRE Transportista la serie debe empezar con V (ej. V001). Recibido: {req.Serie}");

        // ---- Dates ----
        var issueDate = req.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (req.TransferStartDate < issueDate)
            errors.Add(
                $"La fecha de traslado ({req.TransferStartDate:yyyy-MM-dd}) no puede ser anterior a la fecha de emisión ({issueDate:yyyy-MM-dd}).");

        // ---- Transfer reason ----
        if (!ValidTransferReasons.Contains(req.TransferReasonCode))
            errors.Add($"Motivo de traslado '{req.TransferReasonCode}' no está en el catálogo SUNAT.");

        if (string.IsNullOrWhiteSpace(req.TransferReasonDescription))
            errors.Add("La descripción del motivo de traslado es obligatoria.");
        else if (req.TransferReasonDescription.Length > 100)
            errors.Add("La descripción del motivo no debe exceder 100 caracteres.");

        if (!string.IsNullOrEmpty(req.Note) && req.Note.Length > 500)
            errors.Add("Las observaciones no deben exceder 500 caracteres.");

        // ---- Weight + packages ----
        if (req.GrossWeight <= 0)
            errors.Add("El peso bruto debe ser mayor a 0.");

        if (!string.IsNullOrEmpty(req.WeightUnitCode) && !ValidWeightUnits.Contains(req.WeightUnitCode))
            errors.Add($"Unidad de peso '{req.WeightUnitCode}' inválida. Usa KGM, TNE o GRM.");

        if (req.TotalPackages < 1)
            errors.Add("El número de bultos debe ser al menos 1.");

        // ---- Transport mode ----
        if (!ValidTransportModes.Contains(req.TransportMode))
            errors.Add($"Modalidad de transporte '{req.TransportMode}' inválida. Debe ser 01 (Público) o 02 (Privado).");

        // Conductor obligatorio para transporte privado
        if (req.TransportMode == "02")
        {
            if (string.IsNullOrWhiteSpace(req.DriverDocNumber))
                errors.Add("El DNI del conductor es obligatorio para transporte privado.");
            else if (!DigitsOnlyRe.IsMatch(req.DriverDocNumber) || req.DriverDocNumber.Length != 8)
                errors.Add("El DNI del conductor debe tener 8 dígitos.");

            if (string.IsNullOrWhiteSpace(req.DriverName))
                errors.Add("El nombre del conductor es obligatorio para transporte privado.");

            if (string.IsNullOrWhiteSpace(req.VehiclePlate))
                errors.Add("La placa del vehículo es obligatoria para transporte privado.");
            else if (!PlateRe.IsMatch(req.VehiclePlate.ToUpperInvariant().Replace(" ", "")))
                errors.Add($"Placa '{req.VehiclePlate}' inválida. Formato esperado ABC-123 o similar.");
        }

        // Transportista obligatorio para transporte público
        if (req.TransportMode == "01")
        {
            if (string.IsNullOrWhiteSpace(req.CarrierDocNumber))
                errors.Add("El RUC del transportista es obligatorio para transporte público.");
            else if (!DigitsOnlyRe.IsMatch(req.CarrierDocNumber) || req.CarrierDocNumber.Length != 11)
                errors.Add("El RUC del transportista debe tener 11 dígitos.");
            else if (!IsValidRuc(req.CarrierDocNumber))
                errors.Add($"El RUC del transportista {req.CarrierDocNumber} no pasa la verificación SUNAT (mod-11).");

            if (string.IsNullOrWhiteSpace(req.CarrierName))
                errors.Add("La razón social del transportista es obligatoria para transporte público.");

            if (!string.IsNullOrEmpty(req.CarrierDocType) && !ValidCarrierDocTypes.Contains(req.CarrierDocType))
                errors.Add($"Tipo de documento del transportista '{req.CarrierDocType}' inválido.");
        }

        // ---- Recipient ----
        if (!ValidRecipientDocTypes.Contains(req.RecipientDocType))
            errors.Add($"Tipo de documento del destinatario '{req.RecipientDocType}' inválido.");
        else
        {
            switch (req.RecipientDocType)
            {
                case "6": // RUC
                    if (string.IsNullOrWhiteSpace(req.RecipientDocNumber) ||
                        req.RecipientDocNumber.Length != 11 ||
                        !DigitsOnlyRe.IsMatch(req.RecipientDocNumber))
                        errors.Add("El RUC del destinatario debe tener 11 dígitos.");
                    else if (!IsValidRuc(req.RecipientDocNumber))
                        errors.Add($"El RUC del destinatario {req.RecipientDocNumber} no pasa la verificación SUNAT (mod-11).");
                    break;
                case "1": // DNI
                    if (string.IsNullOrWhiteSpace(req.RecipientDocNumber) ||
                        req.RecipientDocNumber.Length != 8 ||
                        !DigitsOnlyRe.IsMatch(req.RecipientDocNumber))
                        errors.Add("El DNI del destinatario debe tener 8 dígitos.");
                    break;
                case "0": // Sin documento
                    // Optional, nothing to check.
                    break;
                default: // CE, Pasaporte
                    if (string.IsNullOrWhiteSpace(req.RecipientDocNumber))
                        errors.Add("El número de documento del destinatario es obligatorio.");
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(req.RecipientName))
            errors.Add("El nombre/razón social del destinatario es obligatorio.");
        else if (req.RecipientName.Length > 200)
            errors.Add("El nombre del destinatario no debe exceder 200 caracteres.");

        // ---- Addresses ----
        if (!UbigeoRe.IsMatch(req.OriginUbigeo ?? ""))
            errors.Add($"Ubigeo de origen '{req.OriginUbigeo}' inválido. Debe tener 6 dígitos.");

        if (!UbigeoRe.IsMatch(req.DestinationUbigeo ?? ""))
            errors.Add($"Ubigeo de destino '{req.DestinationUbigeo}' inválido. Debe tener 6 dígitos.");

        if (string.IsNullOrWhiteSpace(req.OriginAddress))
            errors.Add("La dirección de origen es obligatoria.");
        else if (req.OriginAddress.Length > 300)
            errors.Add("La dirección de origen no debe exceder 300 caracteres.");

        if (string.IsNullOrWhiteSpace(req.DestinationAddress))
            errors.Add("La dirección de destino es obligatoria.");
        else if (req.DestinationAddress.Length > 300)
            errors.Add("La dirección de destino no debe exceder 300 caracteres.");

        // ---- Items ----
        if (req.Items is null || req.Items.Count == 0)
            errors.Add("Debe incluir al menos un item.");
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

                if (string.IsNullOrWhiteSpace(item.UnitCode))
                    errors.Add($"{prefix}: el código de unidad es obligatorio (ej. NIU, KGM, LTR).");
                else if (item.UnitCode.Length > 4)
                    errors.Add($"{prefix}: el código de unidad no debe exceder 4 caracteres.");
            }
        }

        return errors;
    }

    /// <summary>
    /// SUNAT mod-11 check digit verification for 11-digit RUC.
    /// Algorithm: sum(digit * weight) % 11, where weights are 5,4,3,2,7,6,5,4,3,2 for the first 10 digits.
    /// </summary>
    public static bool IsValidRuc(string ruc)
    {
        if (string.IsNullOrEmpty(ruc) || ruc.Length != 11 || !DigitsOnlyRe.IsMatch(ruc))
            return false;

        // Only types 10, 15, 17, 20 are real (natural person / business / etc.)
        var typePrefix = ruc.Substring(0, 2);
        if (typePrefix is not "10" and not "15" and not "17" and not "20")
            return false;

        ReadOnlySpan<int> weights = stackalloc int[] { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
        var sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (ruc[i] - '0') * weights[i];

        var rest = sum % 11;
        var check = rest >= 2 ? 11 - rest : rest == 0 ? 0 : 1;
        return check == (ruc[10] - '0');
    }
}
