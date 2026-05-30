using TukiFact.Application.DTOs.Documents;

namespace TukiFact.Application.Validation;

public static class VoidDocumentValidator
{
    public static List<string> Validate(VoidDocumentRequest req)
    {
        var errors = new List<string>();

        if (req.DocumentId == Guid.Empty)
            errors.Add("El identificador del documento a anular es obligatorio.");

        if (string.IsNullOrWhiteSpace(req.VoidReason))
            errors.Add("El motivo de anulación es obligatorio.");
        else if (req.VoidReason.Length is < 5 or > 100)
            errors.Add("El motivo de anulación debe tener entre 5 y 100 caracteres.");

        return errors;
    }
}
