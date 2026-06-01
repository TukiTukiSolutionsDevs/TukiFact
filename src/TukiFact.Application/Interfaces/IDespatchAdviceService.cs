using TukiFact.Application.DTOs.DespatchAdvices;

namespace TukiFact.Application.Interfaces;

public interface IDespatchAdviceService
{
    Task<DespatchAdviceResponse> CreateAsync(CreateDespatchAdviceRequest request, Guid tenantId, Guid userId, CancellationToken ct = default);
    Task<DespatchAdviceResponse> EmitAsync(Guid id, Guid tenantId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Re-poll SUNAT for a GRE that's stuck in 'sent' (ticket assigned but no CDR yet).
    /// Useful when the inline polling timed out during EmitAsync.
    /// </summary>
    Task<DespatchAdviceResponse> RefreshStatusAsync(Guid id, Guid tenantId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Mark an accepted GRE as cancelled. SUNAT's formal Comunicación de Baja flow happens
    /// in the SOL portal; this endpoint records the local cancellation + audit trail.
    /// </summary>
    Task<DespatchAdviceResponse> CancelAsync(Guid id, Guid tenantId, Guid userId, string? reason, CancellationToken ct = default);

    Task<DespatchAdviceResponse?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<(IReadOnlyList<DespatchAdviceResponse> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? documentType = null, string? status = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default);
}
