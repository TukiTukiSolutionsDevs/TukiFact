using TukiFact.Domain.Interfaces;

namespace TukiFact.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private Guid _currentTenantId;

    public Guid GetCurrentTenantId() => _currentTenantId;

    public void SetCurrentTenantId(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }
}
