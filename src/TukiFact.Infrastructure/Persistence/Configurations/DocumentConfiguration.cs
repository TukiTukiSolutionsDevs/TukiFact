using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.DocumentType).HasMaxLength(2).IsRequired();
        builder.Property(d => d.Serie).HasMaxLength(4).IsRequired();
        builder.Property(d => d.Correlative).IsRequired();
        builder.Ignore(d => d.FullNumber); // Computed, not stored

        builder.Property(d => d.IssueDate).IsRequired();
        builder.Property(d => d.Currency).HasMaxLength(3).HasDefaultValue("PEN");

        builder.Property(d => d.CustomerDocType).HasMaxLength(1).IsRequired();
        builder.Property(d => d.CustomerDocNumber).HasMaxLength(15).IsRequired();
        builder.Property(d => d.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(d => d.CustomerAddress).HasMaxLength(300);
        builder.Property(d => d.CustomerEmail).HasMaxLength(255);

        builder.Property(d => d.OperacionGravada).HasPrecision(14, 2);
        builder.Property(d => d.OperacionExonerada).HasPrecision(14, 2);
        builder.Property(d => d.OperacionInafecta).HasPrecision(14, 2);
        builder.Property(d => d.OperacionGratuita).HasPrecision(14, 2);
        builder.Property(d => d.Igv).HasPrecision(14, 2);
        builder.Property(d => d.TotalDescuento).HasPrecision(14, 2);
        builder.Property(d => d.Total).HasPrecision(14, 2);

        builder.Property(d => d.Status).HasMaxLength(20).HasDefaultValue("draft");
        builder.Property(d => d.SunatResponseCode).HasMaxLength(10);
        builder.Property(d => d.HashCode).HasMaxLength(100);

        builder.Property(d => d.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(d => d.UpdatedAt).HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(d => new { d.TenantId, d.Serie, d.Correlative }).IsUnique();
        builder.HasIndex(d => new { d.TenantId, d.Status });
        builder.HasIndex(d => new { d.TenantId, d.IssueDate });

        // Relationships
        builder.HasOne(d => d.Tenant).WithMany(t => t.Documents)
            .HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(d => d.SeriesNav).WithMany()
            .HasForeignKey(d => d.SeriesId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(d => d.Items).WithOne(i => i.Document)
            .HasForeignKey(i => i.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.XmlLogs).WithOne(l => l.Document)
            .HasForeignKey(l => l.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}
