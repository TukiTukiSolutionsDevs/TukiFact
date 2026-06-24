namespace TukiFact.Application.Interfaces;

/// <summary>
/// Validates RUC/DNI against apis.net.pe (PeruDevs).
/// Uses a single central API key configured in ApisNetPe:ApiKey — DNI/RUC lookup
/// is a premium feature included in every plan, no BYOK.
/// </summary>
public interface IRucValidationService
{
    Task<RucInfo?> ValidateRucAsync(string ruc, CancellationToken ct = default);
    Task<DniInfo?> ValidateDniAsync(string dni, CancellationToken ct = default);
}

public record RucInfo(
    string Ruc,
    string RazonSocial,
    string Estado, // ACTIVO, BAJA, etc.
    string Condicion, // HABIDO, NO HABIDO
    string? Direccion,
    string? Ubigeo,
    string? Departamento,
    string? Provincia,
    string? Distrito
);

public record DniInfo(
    string Dni,
    string Nombres,
    string ApellidoPaterno,
    string ApellidoMaterno,
    string NombreCompleto
);
