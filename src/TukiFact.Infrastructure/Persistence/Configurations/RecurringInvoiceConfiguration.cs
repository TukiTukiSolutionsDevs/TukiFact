using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class RecurringInvoiceConfiguration : IEntityTypeConfiguration<RecurringInvoice>
{
    public void Configure(EntityTypeBuilder<RecurringInvoice> builder)
    {
        builder.ToTable("recurring_invoices");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.DocumentType).HasMaxLength(2).IsRequired();
        builder.Property(r => r.Serie).HasMaxLength(4).IsRequired();

        // Customer
        builder.Property(r => r.CustomerDocType).HasMaxLength(1).IsRequired();
        builder.Property(r => r.CustomerDocNumber).HasMaxLength(15).IsRequired();
        builder.Property(r => r.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.CustomerAddress).HasMaxLength(300);
        builder.Property(r => r.CustomerEmail).HasMaxLength(200);

        // Items JSON
        builder.Property(r => r.ItemsJson).HasColumnType("jsonb").IsRequired();

        // Currency
        builder.Property(r => r.Currency).HasMaxLength(3).HasDefaultValue("PEN");

        // Scheduling
        builder.Property(r => r.Frequency).HasMaxLength(20).IsRequired();
        builder.Property(r => r.StartDate).IsRequired();

        // Status
        builder.Property(r => r.Status).HasMaxLength(20).HasDefaultValue("active");

        // Notes
        builder.Property(r => r.Notes).HasMaxLength(500);

        // Audit
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(r => new { r.TenantId, r.Status });
        builder.HasIndex(r => r.NextEmissionDate);

        // Relationships
        builder.HasOne(r => r.Tenant).WithMany()
            .HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
