using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Validates CPE (Comprobante de Pago Electrónico) via SUNAT REST API.
/// Uses same OAuth2 credentials as GRE (generated in SOL menu).
/// URL: https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/validar
/// </summary>
public class CpeValidationService : ICpeValidationService
{
    private readonly ILogger<CpeValidationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CpeValidationService(
        ILogger<CpeValidationService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("SunatGre");
        _configuration = configuration;
    }

    public async Task<CpeValidationResult?> ValidateCpeAsync(
        string rucEmisor, string tipoCpe, string serie, string correlativo,
        DateOnly fechaEmision, decimal total,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating CPE {Ruc}-{Tipo}-{Serie}-{Correlativo}",
            rucEmisor, tipoCpe, serie, correlativo);

        try
        {
            var environment = _configuration["Sunat:Environment"] ?? "beta";
            var baseUrl = environment == "production"
                ? "https://api-cpe.sunat.gob.pe"
                : "https://api-cpe-beta.sunat.gob.pe";

            var url = $"{baseUrl}/v1/contribuyente/gem/comprobantes/" +
                      $"{rucEmisor}-{tipoCpe}-{serie}-{correlativo}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Note: OAuth2 token should be added via middleware or per-request
            // For now, this validates the structure — token injection will come from tenant config

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CPE validation failed: {Status}", response.StatusCode);
                return new CpeValidationResult("0", "ERROR_CONSULTA", response.StatusCode.ToString(), null);
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            var estado = GetString(root, "estadoCp") ?? "0";
            var estadoDesc = estado switch
            {
                "1" => "ACEPTADO",
                "2" => "ANULADO",
                "3" => "AUTORIZADO",
                "4" => "NO EXISTE",
                _ => "DESCONOCIDO"
            };

            return new CpeValidationResult(
                Estado: estado,
                EstadoDesc: estadoDesc,
                CodigoError: GetString(root, "codError"),
                MensajeError: GetString(root, "msgError")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating CPE");
            return null;
        }
    }

    private static string? GetString(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var prop) ? prop.GetString() : null;
    }
}
