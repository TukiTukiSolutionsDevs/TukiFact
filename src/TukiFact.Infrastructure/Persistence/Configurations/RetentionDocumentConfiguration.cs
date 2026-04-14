using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class RetentionDocumentConfiguration : IEntityTypeConfiguration<RetentionDocument>
{
    public void Configure(EntityTypeBuilder<RetentionDocument> builder)
    {
        builder.ToTable("retention_documents");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.DocumentType).HasMaxLength(2).IsRequired();
        builder.Property(r => r.Serie).HasMaxLength(4).IsRequired();
        builder.Property(r => r.Correlative).IsRequired();
        builder.Ignore(r => r.FullNumber);

        builder.Property(r => r.IssueDate).IsRequired();

        // Supplier
        builder.Property(r => r.SupplierDocType).HasMaxLength(1).IsRequired();
        builder.Property(r => r.SupplierDocNumber).HasMaxLength(15).IsRequired();
        builder.Property(r => r.SupplierName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.SupplierAddress).HasMaxLength(300);

        // Regime
        builder.Property(r => r.RegimeCode).HasMaxLength(2).IsRequired();
        builder.Property(r => r.RetentionPercent).HasPrecision(5, 2).IsRequired();

        // Totals
        builder.Property(r => r.TotalInvoiceAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(r => r.TotalRetained).HasPrecision(14, 2).IsRequired();
        builder.Property(r => r.TotalPaid).HasPrecision(14, 2).IsRequired();
        builder.Property(r => r.Currency).HasMaxLength(3).HasDefaultValue("PEN");

        builder.Property(r => r.Notes).HasMaxLength(500);

        // SUNAT
        builder.Property(r => r.Status).HasMaxLength(20).HasDefaultValue("draft");
        builder.Property(r => r.SunatResponseCode).HasMaxLength(10);
        builder.Property(r => r.SunatResponseDescription).HasMaxLength(500);
        builder.Property(r => r.HashCode).HasMaxLength(100);

        // Files
        builder.Property(r => r.XmlUrl).HasMaxLength(500);
        builder.Property(r => r.PdfUrl).HasMaxLength(500);
        builder.Property(r => r.CdrUrl).HasMaxLength(500);

        // Audit
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(r => new { r.TenantId, r.Serie, r.Correlative }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.IssueDate });

        // Relationships
        builder.HasOne(r => r.Tenant).WithMany()
            .HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.References).WithOne(ref_ => ref_.RetentionDocument)
            .HasForeignKey(ref_ => ref_.RetentionDocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RetentionDocumentReferenceConfiguration : IEntityTypeConfiguration<RetentionDocumentReference>
{
    public void Configure(EntityTypeBuilder<RetentionDocumentReference> builder)
    {
        builder.ToTable("retention_document_references");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.DocumentType).HasMaxLength(2).IsRequired();
        builder.Property(r => r.DocumentNumber).HasMaxLength(20).IsRequired();
        builder.Property(r => r.DocumentDate).IsRequired();
        builder.Property(r => r.InvoiceAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(r => r.InvoiceCurrency).HasMaxLength(3).HasDefaultValue("PEN");

        builder.Property(r => r.PaymentDate).IsRequired();
        builder.Property(r => r.PaymentNumber).HasDefaultValue(1);
        builder.Property(r => r.PaymentAmount).HasPrecision(14, 2).IsRequired();

        builder.Property(r => r.RetainedAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(r => r.NetPaidAmount).HasPrecision(14, 2).IsRequired();

        builder.Property(r => r.ExchangeRate).HasPrecision(10, 4);

        builder.HasIndex(r => r.RetentionDocumentId);
    }
}
