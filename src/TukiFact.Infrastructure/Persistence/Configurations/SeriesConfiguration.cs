using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.ToTable("series");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.DocumentType).HasMaxLength(2).IsRequired();
        builder.Property(s => s.Serie).HasMaxLength(4).IsRequired();
        builder.Property(s => s.CurrentCorrelative).HasDefaultValue(0L).IsRequired();
        builder.Property(s => s.EmissionPoint).HasMaxLength(50).HasDefaultValue("PRINCIPAL");
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(s => new { s.TenantId, s.DocumentType, s.Serie }).IsUnique();

        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.Series)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
