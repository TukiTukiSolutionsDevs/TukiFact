using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotations");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(q => q.QuotationNumber).HasMaxLength(20).IsRequired();
        builder.Property(q => q.Correlative).IsRequired();

        builder.Property(q => q.IssueDate).IsRequired();
        builder.Property(q => q.ValidUntil).IsRequired();

        // Customer
        builder.Property(q => q.CustomerDocType).HasMaxLength(1).IsRequired();
        builder.Property(q => q.CustomerDocNumber).HasMaxLength(15).IsRequired();
        builder.Property(q => q.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(q => q.CustomerAddress).HasMaxLength(300);
        builder.Property(q => q.CustomerEmail).HasMaxLength(200);
        builder.Property(q => q.CustomerPhone).HasMaxLength(20);

        // Currency
        builder.Property(q => q.Currency).HasMaxLength(3).HasDefaultValue("PEN");

        // Amounts
        builder.Property(q => q.Subtotal).HasPrecision(14, 2).IsRequired();
        builder.Property(q => q.Igv).HasPrecision(14, 2).IsRequired();
        builder.Property(q => q.Total).HasPrecision(14, 2).IsRequired();
        builder.Property(q => q.TotalDiscount).HasPrecision(14, 2).HasDefaultValue(0m);

        // Status
        builder.Property(q => q.Status).HasMaxLength(20).HasDefaultValue("draft");

        // Invoice reference
        builder.Property(q => q.InvoiceDocumentNumber).HasMaxLength(20);

        // Notes
        builder.Property(q => q.Notes).HasMaxLength(1000);
        builder.Property(q => q.TermsAndConditions).HasMaxLength(2000);

        // Files
        builder.Property(q => q.PdfUrl).HasMaxLength(500);

        // Audit
        builder.Property(q => q.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(q => q.UpdatedAt).HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(q => new { q.TenantId, q.QuotationNumber }).IsUnique();
        builder.HasIndex(q => new { q.TenantId, q.Status });
        builder.HasIndex(q => new { q.TenantId, q.IssueDate });

        // Relationships
        builder.HasOne(q => q.Tenant).WithMany()
            .HasForeignKey(q => q.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(q => q.InvoiceDocument).WithMany()
            .HasForeignKey(q => q.InvoiceDocumentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(q => q.Items).WithOne(i => i.Quotation)
            .HasForeignKey(i => i.QuotationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuotationItemConfiguration : IEntityTypeConfiguration<QuotationItem>
{
    public void Configure(EntityTypeBuilder<QuotationItem> builder)
    {
        builder.ToTable("quotation_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.Sequence).IsRequired();
        builder.Property(i => i.ProductCode).HasMaxLength(30);
        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Quantity).HasPrecision(14, 4).IsRequired();
        builder.Property(i => i.UnitMeasure).HasMaxLength(5).HasDefaultValue("NIU");
        builder.Property(i => i.UnitPrice).HasPrecision(14, 4).IsRequired();
        builder.Property(i => i.Discount).HasPrecision(14, 2).HasDefaultValue(0m);
        builder.Property(i => i.IgvType).HasMaxLength(2).HasDefaultValue("10");
        builder.Property(i => i.IgvAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(i => i.Subtotal).HasPrecision(14, 2).IsRequired();
        builder.Property(i => i.Total).HasPrecision(14, 2).IsRequired();

        builder.HasIndex(i => i.QuotationId);
    }
}
