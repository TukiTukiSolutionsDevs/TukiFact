using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class SunatCatalogCodeConfiguration : IEntityTypeConfiguration<SunatCatalogCode>
{
    public void Configure(EntityTypeBuilder<SunatCatalogCode> builder)
    {
        builder.ToTable("sunat_catalog_codes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.CatalogNumber).HasMaxLength(5).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500).IsRequired();
        builder.Property(c => c.IsActive).HasDefaultValue(true);
        builder.Property(c => c.SortOrder).HasDefaultValue(0);

        builder.HasIndex(c => new { c.CatalogNumber, c.Code }).IsUnique();
    }
}
