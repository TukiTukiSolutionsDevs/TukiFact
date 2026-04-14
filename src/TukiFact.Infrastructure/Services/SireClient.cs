using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// REST client for SUNAT SIRE (Sistema Integrado de Registros Electrónicos).
/// Uses same OAuth2 token endpoint as GRE.
/// IMPORTANT: CORS blocked by SUNAT — always call from backend, never from browser.
/// Reference: R.S. 112-2021, R.S. 040-2022, R.S. 293-2024
/// </summary>
public class SireClient : ISireClient
{
    private readonly ILogger<SireClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _environment;

    // SIRE API endpoints
    private const string TokenUrl = "https://api-seguridad.sunat.gob.pe/v1/clientessol/{0}/oauth2/token/";
    private const string SireBaseUrl = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv";
    private const string SireBetaBaseUrl = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv"; // Same URL, different behavior in beta

    public SireClient(IConfiguration configuration, ILogger<SireClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _environment = configuration["Sunat:Environment"] ?? "beta";
        _httpClient = httpClientFactory.CreateClient("SunatSire");
    }

    public async Task<string> GetTokenAsync(string ruc, string solUser, string solPassword,
        string clientId, string clientSecret, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting SIRE OAuth2 token for RUC {Ruc}", ruc);

        if (_environment == "beta")
        {
            _logger.LogInformation("SIRE BETA: Returning stub token");
            return "beta-stub-token-sire";
        }

        var tokenEndpoint = string.Format(TokenUrl, clientId);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["scope"] = "https://api-sire.sunat.gob.pe",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["username"] = $"{ruc}{solUser}",
            ["password"] = solPassword
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SIRE token request failed: {Status} {Body}", response.StatusCode, json);
            throw new InvalidOperationException($"SIRE token failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("No access_token in SIRE response");
    }

    public async Task<SireProposalResponse> GetProposalAsync(string token, string ruc,
        string period, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting SIRE proposal for {Ruc} period {Period}", ruc, period);

        if (_environment == "beta")
        {
            return new SireProposalResponse(true, period, "PROPUESTA", 0, null, null);
        }

        var url = $"{SireBaseUrl}/contribuyente/{ruc}/periodos/{period}/propuesta";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SIRE proposal failed: {Status} {Body}", response.StatusCode, json);
            return new SireProposalResponse(false, period, null, null, null, json);
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new SireProposalResponse(
            true,
            period,
            root.TryGetProperty("desEstado", out var est) ? est.GetString() : "PROPUESTA",
            root.TryGetProperty("numTotalRegistros", out var total) ? total.GetInt32() : 0,
            null,
            null
        );
    }

    public async Task<SireTicketResponse> AcceptProposalAsync(string token, string ruc,
        string period, CancellationToken ct = default)
    {
        _logger.LogInformation("Accepting SIRE proposal for {Ruc} period {Period}", ruc, period);

        if (_environment == "beta")
        {
            return new SireTicketResponse(true, $"TICKET-SIRE-{period}-001", null);
        }

        var url = $"{SireBaseUrl}/contribuyente/{ruc}/periodos/{period}/propuesta/aceptar";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SIRE accept failed: {Status} {Body}", response.StatusCode, json);
            return new SireTicketResponse(false, null, json);
        }

        using var doc = JsonDocument.Parse(json);
        var ticket = doc.RootElement.TryGetProperty("numTicket", out var t) ? t.GetString() : null;

        return new SireTicketResponse(true, ticket, null);
    }

    public async Task<SireTicketResponse> UploadReplacementAsync(string token, string ruc,
        string period, byte[] fileContent, CancellationToken ct = default)
    {
        _logger.LogInformation("Uploading SIRE replacement for {Ruc} period {Period}", ruc, period);

        if (_environment == "beta")
        {
            return new SireTicketResponse(true, $"TICKET-SIRE-REPL-{period}-001", null);
        }

        var url = $"{SireBaseUrl}/contribuyente/{ruc}/periodos/{period}/rvie/reemplazo";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(fileContent), "archivo", $"RVIE-{ruc}-{period}.txt");
        request.Content = multipart;

        var response = await _httpClient.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SIRE upload failed: {Status} {Body}", response.StatusCode, json);
            return new SireTicketResponse(false, null, json);
        }

        using var doc = JsonDocument.Parse(json);
        var ticket = doc.RootElement.TryGetProperty("numTicket", out var t) ? t.GetString() : null;

        return new SireTicketResponse(true, ticket, null);
    }

    public async Task<SireTicketStatusResponse> GetTicketStatusAsync(string token, string ruc,
        string ticket, CancellationToken ct = default)
    {
        _logger.LogInformation("Checking SIRE ticket {Ticket} for {Ruc}", ticket, ruc);

        if (_environment == "beta")
        {
            return new SireTicketStatusResponse(true, "TERMINADO", "Proceso completado", 10, 0, null);
        }

        var url = $"{SireBaseUrl}/contribuyente/{ruc}/tickets/{ticket}/estado";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            return new SireTicketStatusResponse(false, null, null, null, null, json);
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new SireTicketStatusResponse(
            true,
            root.TryGetProperty("codEstado", out var s) ? s.GetString() : null,
            root.TryGetProperty("desEstado", out var d) ? d.GetString() : null,
            root.TryGetProperty("numAceptados", out var a) ? a.GetInt32() : null,
            root.TryGetProperty("numRechazados", out var r) ? r.GetInt32() : null,
            null
        );
    }

    public async Task<byte[]> DownloadReportAsync(string token, string ruc,
        string period, string format, CancellationToken ct = default)
    {
        _logger.LogInformation("Downloading SIRE report for {Ruc} period {Period} format {Format}", ruc, period, format);

        if (_environment == "beta")
        {
            return Encoding.UTF8.GetBytes($"SIRE Beta Report — {ruc} — {period}");
        }

        var url = $"{SireBaseUrl}/contribuyente/{ruc}/periodos/{period}/reporte?formato={format}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"SIRE download failed: {error}");
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
