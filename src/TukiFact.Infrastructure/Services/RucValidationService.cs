using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Validates RUC/DNI via apis.net.pe (default) or peruapi.com.
/// Free tier: 100 requests/day. Uses Bearer token auth.
/// </summary>
public class RucValidationService : IRucValidationService
{
    private readonly ILogger<RucValidationService> _logger;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.apis.net.pe/v2/sunat";

    public RucValidationService(ILogger<RucValidationService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ApisNetPe");
    }

    public async Task<RucInfo?> ValidateRucAsync(string ruc, string? apiKey = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ruc) || ruc.Length != 11)
            return null;

        _logger.LogInformation("Validating RUC {Ruc}", ruc);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/ruc?numero={ruc}");
            if (!string.IsNullOrEmpty(apiKey))
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("RUC validation failed: {Status}", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            return new RucInfo(
                Ruc: GetString(root, "ruc") ?? ruc,
                RazonSocial: GetString(root, "razonSocial") ?? "",
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

    public async Task<DniInfo?> ValidateDniAsync(string dni, string? apiKey = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(dni) || dni.Length != 8)
            return null;

        _logger.LogInformation("Validating DNI {Dni}", dni);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/dni?numero={dni}");
            if (!string.IsNullOrEmpty(apiKey))
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DNI validation failed: {Status}", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            var nombres = GetString(root, "nombres") ?? "";
            var apPaterno = GetString(root, "apellidoPaterno") ?? "";
            var apMaterno = GetString(root, "apellidoMaterno") ?? "";

            return new DniInfo(
                Dni: dni,
                Nombres: nombres,
                ApellidoPaterno: apPaterno,
                ApellidoMaterno: apMaterno,
                NombreCompleto: $"{nombres} {apPaterno} {apMaterno}".Trim()
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
