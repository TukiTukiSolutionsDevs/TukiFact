namespace TukiFact.Application.Interfaces;

/// <summary>
/// Validates RUC/DNI against external APIs (apis.net.pe, peruapi.com).
/// Provider is configured per-tenant in TenantServiceConfig.LookupProvider.
/// </summary>
public interface IRucValidationService
{
    Task<RucInfo?> ValidateRucAsync(string ruc, string? apiKey = null, CancellationToken ct = default);
    Task<DniInfo?> ValidateDniAsync(string dni, string? apiKey = null, CancellationToken ct = default);
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
