using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class VoidedDocumentRepository : IVoidedDocumentRepository
{
    private readonly AppDbContext _context;

    public VoidedDocumentRepository(AppDbContext context) => _context = context;

    public async Task<VoidedDocument?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _context.VoidedDocuments.FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<VoidedDocument>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.VoidedDocuments
            .Where(v => v.TenantId == tenantId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct);

    public async Task<VoidedDocument> CreateAsync(VoidedDocument doc, CancellationToken ct = default)
    {
        await _context.VoidedDocuments.AddAsync(doc, ct);
        await _context.SaveChangesAsync(ct);
        return doc;
    }

    public async Task UpdateAsync(VoidedDocument doc, CancellationToken ct = default)
    {
        doc.UpdatedAt = DateTimeOffset.UtcNow;
        _context.VoidedDocuments.Update(doc);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> GetNextTicketNumberAsync(Guid tenantId, string ticketType, DateOnly date, CancellationToken ct = default)
    {
        var count = await _context.VoidedDocuments
            .CountAsync(v => v.TenantId == tenantId && v.TicketType == ticketType && v.IssueDate == date, ct);
        return count + 1;
    }

    public async Task CreateWithTicketAsync(VoidedDocument entity, CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var lockSource = $"{entity.TenantId}:{entity.TicketType}:{entity.IssueDate:yyyyMMdd}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({lockSource}))", ct);

            var count = await _context.VoidedDocuments
                .CountAsync(v => v.TenantId == entity.TenantId
                                 && v.TicketType == entity.TicketType
                                 && v.IssueDate == entity.IssueDate, ct);
            var seq = count + 1;
            entity.TicketNumber = $"{entity.TicketType}-{entity.IssueDate:yyyyMMdd}-{seq:D3}";

            await _context.VoidedDocuments.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        });
    }

    public async Task<IReadOnlyList<VoidedDocument>> GetPendingForWorkerAsync(int maxRetries, int batchSize, CancellationToken ct = default)
        => await _context.VoidedDocuments
            .Where(v => v.Status == "pending" && v.RetryCount < maxRetries)
            .OrderBy(v => v.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<VoidedDocument>> GetSentForPollAsync(int pollEverySeconds, int batchSize, CancellationToken ct = default)
    {
        var threshold = DateTimeOffset.UtcNow.AddSeconds(-pollEverySeconds);
        return await _context.VoidedDocuments
            .Where(v => v.Status == "sent"
                        && v.SunatTicket != null
                        && (v.LastPolledAt == null || v.LastPolledAt < threshold))
            .OrderBy(v => v.LastPolledAt ?? v.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }
}
