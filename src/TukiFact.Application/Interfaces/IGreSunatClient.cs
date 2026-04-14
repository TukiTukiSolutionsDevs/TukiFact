namespace TukiFact.Application.Interfaces;

/// <summary>
/// REST client for GRE (Guía de Remisión Electrónica) — uses OAuth2, NOT SOAP.
/// </summary>
public interface IGreSunatClient
{
    /// <summary>
    /// Get OAuth2 token for GRE REST API using SOL credentials.
    /// Token expires in 1 hour.
    /// </summary>
    Task<string> GetTokenAsync(string clientId, string clientSecret,
        string ruc, string solUser, string solPassword, CancellationToken ct = default);

    /// <summary>
    /// Send signed DespatchAdvice XML to SUNAT GRE REST API.
    /// Returns ticket number for async status check.
    /// </summary>
    Task<GreSunatResponse> SendDespatchAdviceAsync(string token, string ruc,
        string documentType, string serie, long correlative,
        byte[] signedXmlZip, CancellationToken ct = default);

    /// <summary>
    /// Check GRE ticket status (async processing by SUNAT).
    /// </summary>
    Task<GreSunatResponse> GetTicketStatusAsync(string token, string ruc,
        string ticket, CancellationToken ct = default);
}

public record GreSunatResponse(
    bool Success,
    string? Ticket,
    string? ResponseCode,
    string? Description,
    byte[]? CdrZip,
    string? ErrorMessage
);
