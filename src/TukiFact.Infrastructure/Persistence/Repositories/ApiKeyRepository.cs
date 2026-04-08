using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly AppDbContext _context;

    public ApiKeyRepository(AppDbContext context) => _context = context;

    public async Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default)
        => await _context.ApiKeys.Include(a => a.Tenant).FirstOrDefaultAsync(a => a.KeyHash == keyHash && a.IsActive, ct);

    public async Task<IReadOnlyList<ApiKey>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.ApiKeys.Where(a => a.TenantId == tenantId).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

    public async Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        await _context.ApiKeys.AddAsync(apiKey, ct);
        await _context.SaveChangesAsync(ct);
        return apiKey;
    }

    public async Task RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var key = await _context.ApiKeys.FindAsync([id], ct);
        if (key is not null)
        {
            key.IsActive = false;
            await _context.SaveChangesAsync(ct);
        }
    }
}
