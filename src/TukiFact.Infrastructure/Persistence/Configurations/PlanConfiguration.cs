using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();
        builder.Property(p => p.PriceMonthly).HasPrecision(10, 2).IsRequired();
        builder.Property(p => p.MaxDocumentsPerMonth).IsRequired();
        builder.Property(p => p.Features).HasColumnType("jsonb").HasDefaultValueSql("'{}'");
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
    }
}
