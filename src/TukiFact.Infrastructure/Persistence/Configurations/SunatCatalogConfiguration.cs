using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class SunatCatalogConfiguration : IEntityTypeConfiguration<SunatCatalog>
{
    public void Configure(EntityTypeBuilder<SunatCatalog> builder)
    {
        builder.ToTable("sunat_catalogs");
        builder.HasKey(c => c.CatalogNumber);
        builder.Property(c => c.CatalogNumber).HasMaxLength(5).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasMany(c => c.Codes).WithOne(cc => cc.Catalog)
            .HasForeignKey(cc => cc.CatalogNumber).OnDelete(DeleteBehavior.Cascade);
    }
}
