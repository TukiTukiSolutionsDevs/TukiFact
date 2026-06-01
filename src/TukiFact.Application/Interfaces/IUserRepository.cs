using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct = default);
    /// <summary>Find user by email globally (cross-tenant). Used for forgot-password.</summary>
    Task<User?> GetByEmailGlobalAsync(string email, CancellationToken ct = default);
    /// <summary>List active users (with their Tenant loaded) that match the given email across all tenants.</summary>
    Task<IReadOnlyList<User>> GetActiveByEmailWithTenantsAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
