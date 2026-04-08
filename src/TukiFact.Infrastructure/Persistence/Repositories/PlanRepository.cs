using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly AppDbContext _context;

    public PlanRepository(AppDbContext context) => _context = context;

    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Plan?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.Plans.FirstOrDefaultAsync(p => p.Name == name, ct);

    public async Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Plans.Where(p => p.IsActive).OrderBy(p => p.PriceMonthly).ToListAsync(ct);

    public async Task<Plan> CreateAsync(Plan plan, CancellationToken ct = default)
    {
        await _context.Plans.AddAsync(plan, ct);
        await _context.SaveChangesAsync(ct);
        return plan;
    }
}
