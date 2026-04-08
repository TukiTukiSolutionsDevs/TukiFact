using TukiFact.Application.DTOs.Dashboard;

namespace TukiFact.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(Guid tenantId, CancellationToken ct = default);
}
