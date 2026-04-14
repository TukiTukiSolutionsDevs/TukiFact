namespace TukiFact.Application.Interfaces;

public interface IRateLimiter
{
    Task<(bool Allowed, int Remaining, int Limit)> CheckAsync(Guid tenantId, string endpoint, CancellationToken ct = default);
    Task<(bool Allowed, int Used, int Limit)> CheckMonthlyDocumentsAsync(Guid tenantId, CancellationToken ct = default);
}
