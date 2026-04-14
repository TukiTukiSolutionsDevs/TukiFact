using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

public class SunatClient : ISunatClient
{
    private readonly ILogger<SunatClient> _logger;
    private readonly string _environment;
    private readonly HttpClient _httpClient;

    // SUNAT SOAP endpoints
    private const string BetaUrl = "https://e-beta.sunat.gob.pe/ol-ti-itcpe/billService";
    private const string BetaOtrosCpeUrl = "https://e-beta.sunat.gob.pe/ol-ti-itemision-otroscpe-gem-beta/billService";
    private const string ProdFacturaUrl = "https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService";
    private const string ProdGuiaUrl = "https://e-guiaremision.sunat.gob.pe/ol-ti-itemision-guia-gem/billService";
    private const string ProdOtrosCpeUrl = "https://e-factura.sunat.gob.pe/ol-ti-itemision-otroscpe-gem/billService";

    public SunatClient(IConfiguration configuration, ILogger<SunatClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _environment = configuration["Sunat:Environment"] ?? "beta";
        _httpClient = httpClientFactory.CreateClient("Sunat");
    }

    public Task<SunatResponse> SendDocumentAsync(
        string ruc, string documentType, string fullNumber,
        byte[] signedXmlZip, CancellationToken ct = default)
        => SendDocumentAsync(ruc, documentType, fullNumber, signedXmlZip, null!, ct);

    public async Task<SunatResponse> SendDocumentAsync(
        string ruc, string documentType, string fullNumber,
        byte[] signedXmlZip, SunatCredentials credentials, CancellationToken ct = default)
    {
        var env = credentials?.Environment ?? _environment;
        _logger.LogInformation("Sending {FullNumber} to SUNAT ({Env})", fullNumber, env);

        if (env == "beta")
            return await SendBetaStubAsync(documentType, fullNumber, ct);

        var fileName = $"{ruc}-{documentType}-{fullNumber}.zip";
        var base64Zip = Convert.ToBase64String(signedXmlZip);

        var soapBody = credentials is not null
            ? BuildSendBillSoapEnvelopeWithAuth(fileName, base64Zip, credentials.SolUser, credentials.SolPassword)
            : BuildSendBillSoapEnvelope(fileName, base64Zip);
        var endpoint = GetEndpointForEnv(documentType, env);

        try
        {
            var response = await SendSoapRequestAsync(endpoint, soapBody, ct);
            return ParseSendBillResponse(response, fullNumber);
        }
        catch (HttpRequestException ex) when (IsTimeoutOrNetwork(ex))
        {
            // Retry with backoff for network/timeout errors (M2.5)
            return await RetryWithBackoffAsync(
                () => SendSoapAndParse(endpoint, soapBody, fullNumber, ct),
                fullNumber, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SUNAT sendBill failed for {FullNumber}", fullNumber);
            return new SunatResponse(false, null, null, null, ex.Message);
        }
    }

    public Task<SunatResponse> SendSummaryAsync(
        string ruc, string ticketNumber, byte[] xmlZip, CancellationToken ct = default)
        => SendSummaryAsync(ruc, ticketNumber, xmlZip, null!, ct);

    public async Task<SunatResponse> SendSummaryAsync(
        string ruc, string ticketNumber, byte[] xmlZip, SunatCredentials credentials, CancellationToken ct = default)
    {
        var env = credentials?.Environment ?? _environment;
        _logger.LogInformation("Sending summary {Ticket} to SUNAT ({Env})", ticketNumber, env);

        if (env == "beta")
        {
            await Task.Delay(200, ct);
            return new SunatResponse(true, "0", $"Resumen {ticketNumber} recibido", null, null);
        }

        var fileName = $"{ruc}-{ticketNumber}.zip";
        var base64Zip = Convert.ToBase64String(xmlZip);

        var soapBody = BuildSendSummarySoapEnvelope(fileName, base64Zip);
        var endpoint = GetEndpointForEnv("RC", env);

        try
        {
            var response = await SendSoapRequestAsync(endpoint, soapBody, ct);
            return ParseSendSummaryResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SUNAT sendSummary failed for {Ticket}", ticketNumber);
            return new SunatResponse(false, null, null, null, ex.Message);
        }
    }

    public Task<SunatResponse> GetStatusAsync(string sunatTicket, CancellationToken ct = default)
        => GetStatusAsync(sunatTicket, null!, ct);

    public async Task<SunatResponse> GetStatusAsync(string sunatTicket, SunatCredentials credentials, CancellationToken ct = default)
    {
        var env = credentials?.Environment ?? _environment;
        _logger.LogInformation("Checking status for ticket {Ticket} ({Env})", sunatTicket, env);

        if (env == "beta")
        {
            await Task.Delay(100, ct);
            return new SunatResponse(true, "0", "Proceso completado correctamente", null, null);
        }

        var soapBody = BuildGetStatusSoapEnvelope(sunatTicket);
        var endpoint = GetEndpointForEnv("01", env); // Use factura endpoint for getStatus

        try
        {
            var response = await SendSoapRequestAsync(endpoint, soapBody, ct);
            return ParseGetStatusResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SUNAT getStatus failed for ticket {Ticket}", sunatTicket);
            return new SunatResponse(false, null, null, null, ex.Message);
        }
    }

    // === SOAP envelope builders ===

    private static string BuildSendBillSoapEnvelope(string fileName, string base64Content)
    {
        return $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:ser="http://service.sunat.gob.pe"
                              xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                <soapenv:Header/>
                <soapenv:Body>
                    <ser:sendBill>
                        <fileName>{fileName}</fileName>
                        <contentFile>{base64Content}</contentFile>
                    </ser:sendBill>
                </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    private static string BuildSendSummarySoapEnvelope(string fileName, string base64Content)
    {
        return $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:ser="http://service.sunat.gob.pe">
                <soapenv:Header/>
                <soapenv:Body>
                    <ser:sendSummary>
                        <fileName>{fileName}</fileName>
                        <contentFile>{base64Content}</contentFile>
                    </ser:sendSummary>
                </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    private static string BuildGetStatusSoapEnvelope(string ticket)
    {
        return $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:ser="http://service.sunat.gob.pe">
                <soapenv:Header/>
                <soapenv:Body>
                    <ser:getStatus>
                        <ticket>{ticket}</ticket>
                    </ser:getStatus>
                </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    // === SOAP request/response ===

    private async Task<string> SendSoapRequestAsync(string url, string soapXml, CancellationToken ct)
    {
        var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "");

        var response = await _httpClient.PostAsync(url, content, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    private SunatResponse ParseSendBillResponse(string soapResponse, string fullNumber)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            XNamespace ser = "http://service.sunat.gob.pe";

            // Check for SOAP fault
            var faultString = doc.Descendants("faultstring").FirstOrDefault()?.Value;
            if (faultString is not null)
                return new SunatResponse(false, null, faultString, null, faultString);

            // Extract applicationResponse (base64 CDR ZIP)
            var appResponse = doc.Descendants(ser + "applicationResponse").FirstOrDefault()?.Value
                ?? doc.Descendants("applicationResponse").FirstOrDefault()?.Value;

            if (appResponse is not null)
            {
                var cdrZip = Convert.FromBase64String(appResponse);
                var (code, description, _) = CdrParser.ParseCdr(cdrZip);
                var success = code == "0" || code.StartsWith("0");
                return new SunatResponse(success, code, description, cdrZip, null);
            }

            return new SunatResponse(false, null, "No applicationResponse in SUNAT reply", null, "Empty response");
        }
        catch (Exception ex)
        {
            return new SunatResponse(false, null, null, null, $"Error parsing SUNAT response: {ex.Message}");
        }
    }

    private SunatResponse ParseSendSummaryResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            var ticket = doc.Descendants("ticket").FirstOrDefault()?.Value;
            if (ticket is not null)
                return new SunatResponse(true, "0", $"Ticket: {ticket}", null, null);

            var fault = doc.Descendants("faultstring").FirstOrDefault()?.Value;
            return new SunatResponse(false, null, fault, null, fault);
        }
        catch (Exception ex)
        {
            return new SunatResponse(false, null, null, null, ex.Message);
        }
    }

    private SunatResponse ParseGetStatusResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            var statusCode = doc.Descendants("statusCode").FirstOrDefault()?.Value;

            if (statusCode == "0" || statusCode == "99") // 0=processed, 99=in progress
            {
                var content = doc.Descendants("content").FirstOrDefault()?.Value;
                byte[]? cdrZip = content is not null ? Convert.FromBase64String(content) : null;

                if (cdrZip is not null)
                {
                    var (code, description, _) = CdrParser.ParseCdr(cdrZip);
                    return new SunatResponse(code == "0", code, description, cdrZip, null);
                }

                return new SunatResponse(statusCode == "99", statusCode,
                    statusCode == "99" ? "En proceso" : "Completado", null, null);
            }

            return new SunatResponse(false, statusCode, "Error en procesamiento", null, null);
        }
        catch (Exception ex)
        {
            return new SunatResponse(false, null, null, null, ex.Message);
        }
    }

    private string GetEndpointForEnv(string documentType, string env) => env switch
    {
        "beta" => documentType is "20" or "40" ? BetaOtrosCpeUrl : BetaUrl,
        "production" => documentType switch
        {
            "09" => ProdGuiaUrl,
            "20" or "40" => ProdOtrosCpeUrl,
            _ => ProdFacturaUrl
        },
        _ => BetaUrl
    };

    // Backwards compat
    private string GetEndpoint(string documentType) => GetEndpointForEnv(documentType, _environment);

    // === WS-Security SOAP envelope (for production with SOL credentials) ===

    private static string BuildSendBillSoapEnvelopeWithAuth(string fileName, string base64Content, string solUser, string solPassword)
    {
        return $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:ser="http://service.sunat.gob.pe"
                              xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                <soapenv:Header>
                    <wsse:Security>
                        <wsse:UsernameToken>
                            <wsse:Username>{solUser}</wsse:Username>
                            <wsse:Password>{solPassword}</wsse:Password>
                        </wsse:UsernameToken>
                    </wsse:Security>
                </soapenv:Header>
                <soapenv:Body>
                    <ser:sendBill>
                        <fileName>{fileName}</fileName>
                        <contentFile>{base64Content}</contentFile>
                    </ser:sendBill>
                </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    // === Retry logic for SUNAT timeouts (M2.5) ===

    private static bool IsTimeoutOrNetwork(Exception ex)
    {
        return ex is HttpRequestException or TaskCanceledException
            || ex.InnerException is System.Net.Sockets.SocketException;
    }

    private async Task<SunatResponse> RetryWithBackoffAsync(
        Func<Task<SunatResponse>> action, string context, CancellationToken ct)
    {
        int[] delays = [5, 15, 45]; // seconds
        foreach (var delay in delays)
        {
            _logger.LogWarning("SUNAT retry for {Context} in {Delay}s", context, delay);
            await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SUNAT retry failed for {Context}", context);
            }
        }
        return new SunatResponse(false, null, null, null, $"SUNAT unreachable after 3 retries for {context}");
    }

    private async Task<SunatResponse> SendSoapAndParse(string endpoint, string soapBody, string fullNumber, CancellationToken ct)
    {
        var response = await SendSoapRequestAsync(endpoint, soapBody, ct);
        return ParseSendBillResponse(response, fullNumber);
    }

    private async Task<SunatResponse> SendBetaStubAsync(string documentType, string fullNumber, CancellationToken ct)
    {
        await Task.Delay(200, ct);
        var typeName = documentType switch
        {
            "01" => "Factura Electrónica",
            "03" => "Boleta de Venta Electrónica",
            "07" => "Nota de Crédito Electrónica",
            "08" => "Nota de Débito Electrónica",
            "20" => "Comprobante de Retención Electrónico",
            "40" => "Comprobante de Percepción Electrónico",
            _ => "Documento"
        };
        _logger.LogInformation("SUNAT BETA STUB: {FullNumber} accepted", fullNumber);
        return new SunatResponse(true, "0", $"La {typeName} {fullNumber}, ha sido aceptada", null, null);
    }
}
