using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Validates RUC + DNI via decolecta.com (sucesor de apis.net.pe).
/// Bearer token está configurado centralmente en el HttpClient "Decolecta".
/// Endpoints: SUNAT en /v1/sunat/ruc/full · RENIEC en /v1/reniec/dni.
/// </summary>
public class RucValidationService : IRucValidationService
{
    private readonly ILogger<RucValidationService> _logger;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.decolecta.com/v1";

    public RucValidationService(ILogger<RucValidationService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Decolecta");
    }

    public async Task<RucInfo?> ValidateRucAsync(string ruc, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ruc) || ruc.Length != 11)
            return null;

        _logger.LogInformation("Validating RUC {Ruc} via decolecta", ruc);

        try
        {
            // `/sunat/ruc/full` devuelve dirección + ubigeo; `/sunat/ruc` solo razón social.
            var response = await _httpClient.GetAsync($"{BaseUrl}/sunat/ruc/full?numero={ruc}", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("RUC validation failed: {Status}", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            return new RucInfo(
                Ruc: GetString(root, "numero_documento") ?? ruc,
                RazonSocial: GetString(root, "razon_social") ?? "",
                Estado: GetString(root, "estado") ?? "DESCONOCIDO",
                Condicion: GetString(root, "condicion") ?? "DESCONOCIDO",
                Direccion: GetString(root, "direccion"),
                Ubigeo: GetString(root, "ubigeo"),
                Departamento: GetString(root, "departamento"),
                Provincia: GetString(root, "provincia"),
                Distrito: GetString(root, "distrito")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating RUC {Ruc}", ruc);
            return null;
        }
    }

    public async Task<DniInfo?> ValidateDniAsync(string dni, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(dni) || dni.Length != 8)
            return null;

        _logger.LogInformation("Validating DNI {Dni} via decolecta", dni);

        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/reniec/dni?numero={dni}", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DNI validation failed: {Status}", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            var nombres = GetString(root, "first_name") ?? "";
            var apPaterno = GetString(root, "first_last_name") ?? "";
            var apMaterno = GetString(root, "second_last_name") ?? "";
            // decolecta devuelve full_name en orden RENIEC ("PATERNO MATERNO NOMBRES").
            // Reconstruimos el orden coloquial "NOMBRES PATERNO MATERNO" para los consumidores.
            var nombreCompleto = $"{nombres} {apPaterno} {apMaterno}".Trim();

            return new DniInfo(
                Dni: GetString(root, "document_number") ?? dni,
                Nombres: nombres,
                ApellidoPaterno: apPaterno,
                ApellidoMaterno: apMaterno,
                NombreCompleto: nombreCompleto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating DNI {Dni}", dni);
            return null;
        }
    }

    private static string? GetString(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var prop) ? prop.GetString() : null;
    }
}
