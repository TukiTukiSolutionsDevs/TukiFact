using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class UbigeoConfiguration : IEntityTypeConfiguration<Ubigeo>
{
    public void Configure(EntityTypeBuilder<Ubigeo> builder)
    {
        builder.ToTable("ubigeo");
        builder.HasKey(u => u.Code);

        builder.Property(u => u.Code).HasMaxLength(6).IsRequired();
        builder.Property(u => u.Department).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Province).HasMaxLength(50).IsRequired();
        builder.Property(u => u.District).HasMaxLength(50).IsRequired();
        builder.Property(u => u.IsActive).HasDefaultValue(true);

        // Indexes for search
        builder.HasIndex(u => u.Department);
        builder.HasIndex(u => new { u.Department, u.Province });
    }
}
