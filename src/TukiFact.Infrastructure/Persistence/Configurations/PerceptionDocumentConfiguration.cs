using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class PerceptionDocumentConfiguration : IEntityTypeConfiguration<PerceptionDocument>
{
    public void Configure(EntityTypeBuilder<PerceptionDocument> builder)
    {
        builder.ToTable("perception_documents");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.DocumentType).HasMaxLength(2).IsRequired();
        builder.Property(p => p.Serie).HasMaxLength(4).IsRequired();
        builder.Property(p => p.Correlative).IsRequired();
        builder.Ignore(p => p.FullNumber);

        builder.Property(p => p.IssueDate).IsRequired();

        // Customer
        builder.Property(p => p.CustomerDocType).HasMaxLength(1).IsRequired();
        builder.Property(p => p.CustomerDocNumber).HasMaxLength(15).IsRequired();
        builder.Property(p => p.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.CustomerAddress).HasMaxLength(300);

        // Regime
        builder.Property(p => p.RegimeCode).HasMaxLength(2).IsRequired();
        builder.Property(p => p.PerceptionPercent).HasPrecision(5, 2).IsRequired();

        // Totals
        builder.Property(p => p.TotalInvoiceAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(p => p.TotalPerceived).HasPrecision(14, 2).IsRequired();
        builder.Property(p => p.TotalCollected).HasPrecision(14, 2).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).HasDefaultValue("PEN");

        builder.Property(p => p.Notes).HasMaxLength(500);

        // SUNAT
        builder.Property(p => p.Status).HasMaxLength(20).HasDefaultValue("draft");
        builder.Property(p => p.SunatResponseCode).HasMaxLength(10);
        builder.Property(p => p.SunatResponseDescription).HasMaxLength(500);
        builder.Property(p => p.HashCode).HasMaxLength(100);

        // Files
        builder.Property(p => p.XmlUrl).HasMaxLength(500);
        builder.Property(p => p.PdfUrl).HasMaxLength(500);
        builder.Property(p => p.CdrUrl).HasMaxLength(500);

        // Audit
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(p => new { p.TenantId, p.Serie, p.Correlative }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.Status });
        builder.HasIndex(p => new { p.TenantId, p.IssueDate });

        // Relationships
        builder.HasOne(p => p.Tenant).WithMany()
            .HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.References).WithOne(ref_ => ref_.PerceptionDocument)
            .HasForeignKey(ref_ => ref_.PerceptionDocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PerceptionDocumentReferenceConfiguration : IEntityTypeConfiguration<PerceptionDocumentReference>
{
    public void Configure(EntityTypeBuilder<PerceptionDocumentReference> builder)
    {
        builder.ToTable("perception_document_references");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.DocumentType).HasMaxLength(2).IsRequired();
        builder.Property(p => p.DocumentNumber).HasMaxLength(20).IsRequired();
        builder.Property(p => p.DocumentDate).IsRequired();
        builder.Property(p => p.InvoiceAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(p => p.InvoiceCurrency).HasMaxLength(3).HasDefaultValue("PEN");

        builder.Property(p => p.CollectionDate).IsRequired();
        builder.Property(p => p.CollectionNumber).HasDefaultValue(1);
        builder.Property(p => p.CollectionAmount).HasPrecision(14, 2).IsRequired();

        builder.Property(p => p.PerceivedAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(p => p.TotalCollectedAmount).HasPrecision(14, 2).IsRequired();

        builder.Property(p => p.ExchangeRate).HasPrecision(10, 4);

        builder.HasIndex(p => p.PerceptionDocumentId);
    }
}
