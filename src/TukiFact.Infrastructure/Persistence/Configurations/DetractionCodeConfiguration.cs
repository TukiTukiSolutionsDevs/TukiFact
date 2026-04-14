using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class DetractionCodeConfiguration : IEntityTypeConfiguration<DetractionCode>
{
    public void Configure(EntityTypeBuilder<DetractionCode> builder)
    {
        builder.ToTable("detraction_codes");
        builder.HasKey(d => d.Code);
        builder.Property(d => d.Code).HasMaxLength(3).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Percentage).HasPrecision(5, 2).IsRequired();
        builder.Property(d => d.Annex).HasMaxLength(5).IsRequired();
        builder.Property(d => d.IsActive).HasDefaultValue(true);

        builder.HasIndex(d => d.Annex);
    }
}
