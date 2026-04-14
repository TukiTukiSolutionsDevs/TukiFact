using TukiFact.Application.DTOs.DespatchAdvices;

namespace TukiFact.Application.Interfaces;

public interface IDespatchAdviceService
{
    Task<DespatchAdviceResponse> CreateAsync(CreateDespatchAdviceRequest request, Guid tenantId, CancellationToken ct = default);
    Task<DespatchAdviceResponse> EmitAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<DespatchAdviceResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<DespatchAdviceResponse> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? documentType = null, string? status = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default);
}
