namespace TukiFact.Application.Interfaces;

/// <summary>
/// Client for SUNAT SIRE (Sistema Integrado de Registros Electrónicos).
/// Uses OAuth2 REST API (same token endpoint as GRE).
/// </summary>
public interface ISireClient
{
    /// <summary>Get OAuth2 token for SIRE API</summary>
    Task<string> GetTokenAsync(string ruc, string solUser, string solPassword,
        string clientId, string clientSecret, CancellationToken ct = default);

    /// <summary>Download SUNAT proposal for a given period</summary>
    Task<SireProposalResponse> GetProposalAsync(string token, string ruc,
        string period, CancellationToken ct = default);

    /// <summary>Accept SUNAT RVIE proposal as-is</summary>
    Task<SireTicketResponse> AcceptProposalAsync(string token, string ruc,
        string period, CancellationToken ct = default);

    /// <summary>Upload replacement RVIE (when proposal needs corrections)</summary>
    Task<SireTicketResponse> UploadReplacementAsync(string token, string ruc,
        string period, byte[] fileContent, CancellationToken ct = default);

    /// <summary>Check ticket status</summary>
    Task<SireTicketStatusResponse> GetTicketStatusAsync(string token, string ruc,
        string ticket, CancellationToken ct = default);

    /// <summary>Download generated report (PDF/Excel)</summary>
    Task<byte[]> DownloadReportAsync(string token, string ruc,
        string period, string format, CancellationToken ct = default);
}

// Response DTOs for SIRE
public record SireProposalResponse(
    bool Success,
    string? Period,
    string? Status, // "PROPUESTA", "ACEPTADO", "REEMPLAZADO"
    int? TotalRecords,
    byte[]? Content,
    string? ErrorMessage
);

public record SireTicketResponse(
    bool Success,
    string? Ticket,
    string? ErrorMessage
);

public record SireTicketStatusResponse(
    bool Success,
    string? Status, // "EN_PROCESO", "TERMINADO", "ERROR"
    string? Description,
    int? TotalAccepted,
    int? TotalRejected,
    string? ErrorMessage
);
