namespace TukiFact.Domain.Interfaces;

public interface ITenantProvider
{
    Guid GetCurrentTenantId();
    void SetCurrentTenantId(Guid tenantId);
}
