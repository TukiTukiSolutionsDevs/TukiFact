using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class DocumentItemConfiguration : IEntityTypeConfiguration<DocumentItem>
{
    public void Configure(EntityTypeBuilder<DocumentItem> builder)
    {
        builder.ToTable("document_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(i => i.Sequence).IsRequired();
        builder.Property(i => i.ProductCode).HasMaxLength(30);
        builder.Property(i => i.SunatProductCode).HasMaxLength(8);
        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Quantity).HasPrecision(14, 4).IsRequired();
        builder.Property(i => i.UnitMeasure).HasMaxLength(3).HasDefaultValue("NIU");
        builder.Property(i => i.UnitPrice).HasPrecision(14, 4).IsRequired();
        builder.Property(i => i.UnitPriceWithIgv).HasPrecision(14, 4);
        builder.Property(i => i.IgvType).HasMaxLength(2).HasDefaultValue("10");
        builder.Property(i => i.IgvAmount).HasPrecision(14, 2);
        builder.Property(i => i.Subtotal).HasPrecision(14, 2);
        builder.Property(i => i.Discount).HasPrecision(14, 2);
        builder.Property(i => i.Total).HasPrecision(14, 2);

        builder.HasIndex(i => new { i.DocumentId, i.Sequence }).IsUnique();
    }
}
