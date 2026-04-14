namespace TukiFact.Application.Interfaces;

public interface ICpeValidationService
{
    Task<CpeValidationResult?> ValidateCpeAsync(
        string rucEmisor, string tipoCpe, string serie, string correlativo,
        DateOnly fechaEmision, decimal total,
        CancellationToken ct = default);
}

public record CpeValidationResult(
    string Estado,         // "1" = Aceptado, "3" = No existe
    string EstadoDesc,     // "ACEPTADO", "NO EXISTE"
    string? CodigoError,
    string? MensajeError
);
