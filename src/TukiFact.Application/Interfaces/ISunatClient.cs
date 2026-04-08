namespace TukiFact.Application.Interfaces;

public interface ISunatClient
{
    Task<SunatResponse> SendDocumentAsync(string ruc, string documentType, string fullNumber, byte[] signedXmlZip, CancellationToken ct = default);
    Task<SunatResponse> SendSummaryAsync(string ruc, string ticketNumber, byte[] xmlZip, CancellationToken ct = default);
    Task<SunatResponse> GetStatusAsync(string sunatTicket, CancellationToken ct = default);
}

public record SunatResponse(
    bool Success,
    string? ResponseCode,
    string? Description,
    byte[]? CdrZip,
    string? ErrorMessage
);
