using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class VoidedDocumentRepository : IVoidedDocumentRepository
{
    private readonly AppDbContext _context;

    public VoidedDocumentRepository(AppDbContext context) => _context = context;

    public async Task<VoidedDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.VoidedDocuments.FirstOrDefaultAsync(v => v.Id == id, ct);

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
}
