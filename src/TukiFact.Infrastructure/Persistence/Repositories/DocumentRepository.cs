using Microsoft.EntityFrameworkCore;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context) => _context = context;

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<Document?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        => await _context.Documents
            .Include(d => d.Items.OrderBy(i => i.Sequence))
            .Include(d => d.Tenant)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<(IReadOnlyList<Document> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId, int page, int pageSize,
        string? documentType = null, string? status = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var query = _context.Documents
            .Where(d => d.TenantId == tenantId);

        if (!string.IsNullOrEmpty(documentType))
            query = query.Where(d => d.DocumentType == documentType);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);
        if (dateFrom.HasValue)
            query = query.Where(d => d.IssueDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(d => d.IssueDate <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(d => d.Items.OrderBy(i => i.Sequence))
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Document> CreateAsync(Document document, CancellationToken ct = default)
    {
        await _context.Documents.AddAsync(document, ct);
        await _context.SaveChangesAsync(ct);
        return document;
    }

    public async Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        document.UpdatedAt = DateTimeOffset.UtcNow;
        _context.Documents.Update(document);
        await _context.SaveChangesAsync(ct);
    }
}
