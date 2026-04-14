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
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<DespatchAdvice> DespatchAdvices => Set<DespatchAdvice>();
    public DbSet<DespatchAdviceItem> DespatchAdviceItems => Set<DespatchAdviceItem>();
    public DbSet<Ubigeo> Ubigeos => Set<Ubigeo>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // Batch B
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<DetractionCode> DetractionCodes => Set<DetractionCode>();
    public DbSet<SunatCatalog> SunatCatalogs => Set<SunatCatalog>();
    public DbSet<SunatCatalogCode> SunatCatalogCodes => Set<SunatCatalogCode>();

    // Batch C
    public DbSet<RetentionDocument> RetentionDocuments => Set<RetentionDocument>();
    public DbSet<RetentionDocumentReference> RetentionDocumentReferences => Set<RetentionDocumentReference>();
    public DbSet<PerceptionDocument> PerceptionDocuments => Set<PerceptionDocument>();
    public DbSet<PerceptionDocumentReference> PerceptionDocumentReferences => Set<PerceptionDocumentReference>();
    public DbSet<RecurringInvoice> RecurringInvoices => Set<RecurringInvoice>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();

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
