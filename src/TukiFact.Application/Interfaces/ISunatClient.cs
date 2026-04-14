namespace TukiFact.Application.Interfaces;

public interface ISunatClient
{
    Task<SunatResponse> SendDocumentAsync(string ruc, string documentType, string fullNumber, byte[] signedXmlZip, CancellationToken ct = default);
    Task<SunatResponse> SendDocumentAsync(string ruc, string documentType, string fullNumber, byte[] signedXmlZip, SunatCredentials credentials, CancellationToken ct = default);
    Task<SunatResponse> SendSummaryAsync(string ruc, string ticketNumber, byte[] xmlZip, CancellationToken ct = default);
    Task<SunatResponse> SendSummaryAsync(string ruc, string ticketNumber, byte[] xmlZip, SunatCredentials credentials, CancellationToken ct = default);
    Task<SunatResponse> GetStatusAsync(string sunatTicket, CancellationToken ct = default);
    Task<SunatResponse> GetStatusAsync(string sunatTicket, SunatCredentials credentials, CancellationToken ct = default);
}

public record SunatResponse(
    bool Success,
    string? ResponseCode,
    string? Description,
    byte[]? CdrZip,
    string? ErrorMessage
);

/// <summary>
/// Per-tenant SUNAT credentials for production mode.
/// SOL user format: RUC + SOL username (e.g. "20613614509MODDATOS")
/// </summary>
public record SunatCredentials(
    string SolUser,
    string SolPassword,
    string Environment // "beta" or "production"
);
