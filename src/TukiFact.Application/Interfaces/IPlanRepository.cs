using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Plan?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Plan> CreateAsync(Plan plan, CancellationToken ct = default);
}
