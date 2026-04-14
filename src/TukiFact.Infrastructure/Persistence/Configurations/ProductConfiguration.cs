using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.SunatCode).HasMaxLength(20);
        builder.Property(p => p.Description).HasMaxLength(500).IsRequired();
        builder.Property(p => p.UnitPrice).HasPrecision(18, 4);
        builder.Property(p => p.UnitPriceWithIgv).HasPrecision(18, 4);
        builder.Property(p => p.Currency).HasMaxLength(3).HasDefaultValue("PEN");
        builder.Property(p => p.IgvType).HasMaxLength(2).HasDefaultValue("10");
        builder.Property(p => p.UnitMeasure).HasMaxLength(10).HasDefaultValue("NIU");
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.Brand).HasMaxLength(100);
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(p => new { p.TenantId, p.Code }).IsUnique();

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
