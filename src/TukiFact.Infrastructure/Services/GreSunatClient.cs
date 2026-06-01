using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// REST client for SUNAT GRE (Guía de Remisión Electrónica).
/// GRE uses REST API with OAuth2 — NOT SOAP like invoices.
/// Reference: thegreenter/gre-api openapi.yaml, SUNAT Manual GRE.
///
/// Three modes:
///   "beta"        — hits SUNAT's beta gateway (real round-trip, safe to test with).
///   "production"  — hits SUNAT's prod gateway.
///   "stub"        — local-only stubbed responses (no network). Used by unit tests.
/// </summary>
public class GreSunatClient : IGreSunatClient
{
    // SUNAT GRE REST endpoints. Beta and production share the same hosts — what changes is
    // the credentials/RUC used and the OSE designation in the tenant's SUNAT profile.
    // Override via config (Sunat:Gre:TokenUrl / SendUrl / StatusUrl) when SUNAT publishes new URLs.
    private const string DefaultTokenUrl = "https://api-seguridad.sunat.gob.pe/v1/clientessol/{0}/oauth2/token/";
    private const string DefaultSendUrl = "https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/{0}";
    private const string DefaultStatusUrl = "https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/envios/{0}";

    private readonly ILogger<GreSunatClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _environment;
    private readonly string _tokenUrlTemplate;
    private readonly string _sendUrlTemplate;
    private readonly string _statusUrlTemplate;
    private readonly string _scope;

    public GreSunatClient(IConfiguration configuration, ILogger<GreSunatClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _environment = (configuration["Sunat:Environment"] ?? "beta").ToLowerInvariant();
        _httpClient = httpClientFactory.CreateClient("SunatGre");

        _tokenUrlTemplate = configuration["Sunat:Gre:TokenUrl"] ?? DefaultTokenUrl;
        _sendUrlTemplate = configuration["Sunat:Gre:SendUrl"] ?? DefaultSendUrl;
        _statusUrlTemplate = configuration["Sunat:Gre:StatusUrl"] ?? DefaultStatusUrl;
        _scope = configuration["Sunat:Gre:Scope"] ?? "https://api-cpe.sunat.gob.pe";
    }

    public async Task<string> GetTokenAsync(string clientId, string clientSecret,
        string ruc, string solUser, string solPassword, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting GRE OAuth2 token for RUC {Ruc} ({Env})", ruc, _environment);

        // Pure-local stub mode (unit tests).
        if (_environment == "stub")
        {
            _logger.LogWarning("GRE STUB MODE — returning fake token. Set Sunat:Environment to 'beta' or 'production' for real calls.");
            return "stub-token-gre";
        }

        var tokenUrl = string.Format(_tokenUrlTemplate, clientId);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("scope", _scope),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("username", $"{ruc}{solUser}"),
            new KeyValuePair<string, string>("password", solPassword),
        });

        var response = await _httpClient.PostAsync(tokenUrl, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("GRE OAuth2 token failed ({Env}): {Status} {Body}", _environment, response.StatusCode, body);
            throw new InvalidOperationException(
                $"Error obteniendo token GRE ({_environment}): {(int)response.StatusCode} - {body}");
        }

        var json = JsonDocument.Parse(body);
        var token = json.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Token vacío en respuesta GRE OAuth2");

        _logger.LogInformation("GRE OAuth2 token obtained ({Env}), expires_in: {Expires}s",
            _environment,
            json.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : -1);

        return token;
    }

    public async Task<GreSunatResponse> SendDespatchAdviceAsync(string token, string ruc,
        string documentType, string serie, long correlative,
        byte[] signedXmlZip, CancellationToken ct = default)
    {
        var fileName = $"{ruc}-{documentType}-{serie}-{correlative:D8}";
        _logger.LogInformation("Sending GRE {FileName} to SUNAT ({Env})", fileName, _environment);

        if (_environment == "stub")
        {
            return await SendStubAsync(fileName, ct);
        }

        var sendUrl = string.Format(_sendUrlTemplate, fileName);

        var base64Zip = Convert.ToBase64String(signedXmlZip);
        var requestBody = JsonSerializer.Serialize(new
        {
            archivo = new
            {
                nomArchivo = $"{fileName}.zip",
                arcGreZip = base64Zip,
                hashZip = ComputeSha256(signedXmlZip)
            }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, sendUrl)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GRE send failed ({Env}): {Status} {Body}", _environment, response.StatusCode, body);
                return new GreSunatResponse(false, null, ((int)response.StatusCode).ToString(), null, null,
                    $"Error enviando GRE ({_environment}): {(int)response.StatusCode} - {body}");
            }

            var json = JsonDocument.Parse(body);
            var ticket = json.RootElement.TryGetProperty("numTicket", out var tProp) ? tProp.GetString() : null;

            _logger.LogInformation("GRE sent successfully ({Env}), ticket: {Ticket}", _environment, ticket);
            return new GreSunatResponse(true, ticket, null, "GRE enviada, procesando", null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GRE send exception for {FileName}", fileName);
            return new GreSunatResponse(false, null, null, null, null, ex.Message);
        }
    }

    public async Task<GreSunatResponse> GetTicketStatusAsync(string token, string ruc,
        string ticket, CancellationToken ct = default)
    {
        _logger.LogInformation("Checking GRE ticket {Ticket} ({Env})", ticket, _environment);

        if (_environment == "stub")
        {
            await Task.Delay(100, ct);
            return new GreSunatResponse(true, ticket, "0", "GRE aceptada por SUNAT (stub)", null, null);
        }

        var statusUrl = string.Format(_statusUrlTemplate, ticket);

        var request = new HttpRequestMessage(HttpMethod.Get, statusUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GRE getTicket HTTP {Status} ({Env}): {Body}", response.StatusCode, _environment, body);
                return new GreSunatResponse(false, ticket, ((int)response.StatusCode).ToString(), null, null,
                    $"Error consultando ticket: {(int)response.StatusCode} - {body}");
            }

            var json = JsonDocument.Parse(body);
            var codRespuesta = json.RootElement.TryGetProperty("codRespuesta", out var codProp)
                ? codProp.GetString() : null;
            var arcCdr = json.RootElement.TryGetProperty("arcCdr", out var cdrProp)
                ? cdrProp.GetString() : null;

            byte[]? cdrZip = !string.IsNullOrEmpty(arcCdr) ? Convert.FromBase64String(arcCdr) : null;
            var success = codRespuesta == "0" || codRespuesta?.StartsWith("0") == true;
            var description = json.RootElement.TryGetProperty("desRespuesta", out var desProp)
                ? desProp.GetString() : null;

            _logger.LogInformation("GRE ticket {Ticket} status ({Env}): {Code} {Desc}",
                ticket, _environment, codRespuesta, description);

            return new GreSunatResponse(success, ticket, codRespuesta, description, cdrZip, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GRE getTicketStatus exception for {Ticket}", ticket);
            return new GreSunatResponse(false, ticket, null, null, null, ex.Message);
        }
    }

    private static async Task<GreSunatResponse> SendStubAsync(string fileName, CancellationToken ct)
    {
        await Task.Delay(200, ct);
        var stubTicket = $"STUB-{Guid.NewGuid():N}"[..20];
        return new GreSunatResponse(true, stubTicket, "0",
            $"GRE {fileName} aceptada (stub local)", null, null);
    }

    private static string ComputeSha256(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }
}
