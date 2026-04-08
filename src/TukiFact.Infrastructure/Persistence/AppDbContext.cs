using Microsoft.EntityFrameworkCore;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentItem> DocumentItems => Set<DocumentItem>();
    public DbSet<DocumentXmlLog> DocumentXmlLogs => Set<DocumentXmlLog>();
    public DbSet<VoidedDocument> VoidedDocuments => Set<VoidedDocument>();
    public DbSet<WebhookConfig> WebhookConfigs => Set<WebhookConfig>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TenantServiceConfig> TenantServiceConfigs => Set<TenantServiceConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set RLS context before saving
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (tenantId != Guid.Empty)
        {
            // Use FormattableString overload to avoid SQL injection warning (EF1002).
            // PostgreSQL SET LOCAL doesn't accept bind parameters, so we validate
            // that tenantId is a proper Guid (no injection risk) before formatting.
            await Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.current_tenant', {0}, true)",
                [tenantId.ToString()],
                cancellationToken);
        }

        // Auto-set UpdatedAt for entities being modified
        foreach (var entry in ChangeTracker.Entries<Tenant>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
