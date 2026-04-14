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
/// </summary>
public class GreSunatClient : IGreSunatClient
{
    private readonly ILogger<GreSunatClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _environment;

    // GRE REST endpoints
    private const string BetaTokenUrl = "https://gre-beta.sunat.gob.pe/v1/clientessol/{0}/oauth2/token/";
    private const string BetaSendUrl = "https://gre-beta.sunat.gob.pe/v1/contribuyente/gem/comprobantes/{0}";
    private const string BetaStatusUrl = "https://gre-beta.sunat.gob.pe/v1/contribuyente/gem/comprobantes/envios/{0}";

    private const string ProdTokenUrl = "https://api-seguridad.sunat.gob.pe/v1/clientessol/{0}/oauth2/token/";
    private const string ProdSendUrl = "https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/{0}";
    private const string ProdStatusUrl = "https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/envios/{0}";

    public GreSunatClient(IConfiguration configuration, ILogger<GreSunatClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _environment = configuration["Sunat:Environment"] ?? "beta";
        _httpClient = httpClientFactory.CreateClient("SunatGre");
    }

    public async Task<string> GetTokenAsync(string clientId, string clientSecret,
        string ruc, string solUser, string solPassword, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting GRE OAuth2 token for RUC {Ruc} ({Env})", ruc, _environment);

        if (_environment == "beta")
        {
            _logger.LogInformation("GRE BETA: returning stub token");
            return "beta-stub-token-gre";
        }

        var tokenUrl = string.Format(ProdTokenUrl, clientId);
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("scope", "https://api-cpe.sunat.gob.pe"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("username", $"{ruc}{solUser}"),
            new KeyValuePair<string, string>("password", solPassword),
        });

        var response = await _httpClient.PostAsync(tokenUrl, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("GRE OAuth2 token failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Error obteniendo token GRE: {response.StatusCode} - {body}");
        }

        var json = JsonDocument.Parse(body);
        var token = json.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Token vacío en respuesta GRE OAuth2");

        _logger.LogInformation("GRE OAuth2 token obtained, expires_in: {Expires}s",
            json.RootElement.GetProperty("expires_in").GetInt32());

        return token;
    }

    public async Task<GreSunatResponse> SendDespatchAdviceAsync(string token, string ruc,
        string documentType, string serie, long correlative,
        byte[] signedXmlZip, CancellationToken ct = default)
    {
        var fileName = $"{ruc}-{documentType}-{serie}-{correlative:D8}";
        _logger.LogInformation("Sending GRE {FileName} to SUNAT ({Env})", fileName, _environment);

        if (_environment == "beta")
        {
            return await SendBetaStubAsync(fileName, ct);
        }

        var sendUrl = string.Format(ProdSendUrl, fileName);
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
                _logger.LogError("GRE send failed: {Status} {Body}", response.StatusCode, body);
                return new GreSunatResponse(false, null, null, null, null,
                    $"Error enviando GRE: {response.StatusCode} - {body}");
            }

            var json = JsonDocument.Parse(body);
            var ticket = json.RootElement.GetProperty("numTicket").GetString();

            _logger.LogInformation("GRE sent successfully, ticket: {Ticket}", ticket);
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

        if (_environment == "beta")
        {
            await Task.Delay(100, ct);
            return new GreSunatResponse(true, ticket, "0", "GRE aceptada por SUNAT (beta)", null, null);
        }

        var statusUrl = string.Format(ProdStatusUrl, ticket);
        var request = new HttpRequestMessage(HttpMethod.Get, statusUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            var json = JsonDocument.Parse(body);
            var codRespuesta = json.RootElement.TryGetProperty("codRespuesta", out var codProp)
                ? codProp.GetString() : null;
            var arcCdr = json.RootElement.TryGetProperty("arcCdr", out var cdrProp)
                ? cdrProp.GetString() : null;
            var indCdrGenerado = json.RootElement.TryGetProperty("indCdrGenerado", out var indProp)
                && indProp.GetBoolean();

            byte[]? cdrZip = arcCdr is not null ? Convert.FromBase64String(arcCdr) : null;
            var success = codRespuesta == "0" || codRespuesta?.StartsWith("0") == true;
            var description = json.RootElement.TryGetProperty("desRespuesta", out var desProp)
                ? desProp.GetString() : null;

            _logger.LogInformation("GRE ticket {Ticket} status: {Code} {Desc}",
                ticket, codRespuesta, description);

            return new GreSunatResponse(success, ticket, codRespuesta, description, cdrZip, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GRE getTicketStatus exception for {Ticket}", ticket);
            return new GreSunatResponse(false, ticket, null, null, null, ex.Message);
        }
    }

    private async Task<GreSunatResponse> SendBetaStubAsync(string fileName, CancellationToken ct)
    {
        await Task.Delay(200, ct);
        var stubTicket = $"BETA-{Guid.NewGuid():N}"[..20];
        _logger.LogInformation("GRE BETA STUB: {FileName} accepted, ticket: {Ticket}", fileName, stubTicket);
        return new GreSunatResponse(true, stubTicket, "0",
            $"GRE {fileName} ha sido aceptada (beta)", null, null);
    }

    private static string ComputeSha256(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }
}
