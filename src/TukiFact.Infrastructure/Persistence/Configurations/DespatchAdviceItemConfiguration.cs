using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class DespatchAdviceItemConfiguration : IEntityTypeConfiguration<DespatchAdviceItem>
{
    public void Configure(EntityTypeBuilder<DespatchAdviceItem> builder)
    {
        builder.ToTable("despatch_advice_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.LineNumber).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();
        builder.Property(i => i.ProductCode).HasMaxLength(30);
        builder.Property(i => i.Quantity).HasPrecision(14, 4).IsRequired();
        builder.Property(i => i.UnitCode).HasMaxLength(5).HasDefaultValue("NIU");

        // Relationship configured in DespatchAdviceConfiguration
    }
}
