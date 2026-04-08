using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken ct = default);
    Task RevokeAsync(Guid id, CancellationToken ct = default);
}
