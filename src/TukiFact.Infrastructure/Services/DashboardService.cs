using Microsoft.EntityFrameworkCore;
using TukiFact.Application.DTOs.Dashboard;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Enums;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context) => _context = context;

    public async Task<DashboardResponse> GetDashboardAsync(Guid tenantId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var yearStart = new DateOnly(today.Year, 1, 1);

        var docs = _context.Documents.Where(d => d.TenantId == tenantId);

        var todaySummary = await GetSummary(docs.Where(d => d.IssueDate == today), ct);
        var monthSummary = await GetSummary(docs.Where(d => d.IssueDate >= monthStart), ct);
        var yearSummary = await GetSummary(docs.Where(d => d.IssueDate >= yearStart), ct);

        var byType = await docs
            .GroupBy(d => d.DocumentType)
            .Select(g => new DocumentsByType(
                g.Key, DocumentType.GetName(g.Key), g.Count(), g.Sum(d => d.Total)))
            .ToListAsync(ct);

        var byStatus = await docs
            .GroupBy(d => d.Status)
            .Select(g => new DocumentsByStatus(g.Key, g.Count()))
            .ToListAsync(ct);

        // EF Core can't translate DateOnly.Year/Month in GroupBy with record constructors
        // Use anonymous type → materialize → map to record
        var monthlySalesRaw = await docs
            .Where(d => d.IssueDate >= yearStart)
            .GroupBy(d => new { d.IssueDate.Year, d.IssueDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Total = g.Sum(d => d.Total) })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync(ct);

        var monthlySales = monthlySalesRaw
            .Select(m => new MonthlySales(m.Year, m.Month, m.Count, m.Total))
            .ToList();

        return new DashboardResponse(todaySummary, monthSummary, yearSummary, byType, byStatus, monthlySales);
    }

    private static async Task<DashboardSummary> GetSummary(IQueryable<Domain.Entities.Document> query, CancellationToken ct)
    {
        var stats = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Amount = g.Sum(d => d.Total),
                Igv = g.Sum(d => d.Igv),
                Accepted = g.Count(d => d.Status == "accepted"),
                Rejected = g.Count(d => d.Status == "rejected"),
                Pending = g.Count(d => d.Status == "draft" || d.Status == "sent" || d.Status == "signed")
            })
            .FirstOrDefaultAsync(ct);

        return stats is null
            ? new DashboardSummary(0, 0, 0, 0, 0, 0)
            : new DashboardSummary(stats.Total, stats.Amount, stats.Igv, stats.Accepted, stats.Rejected, stats.Pending);
    }
}
