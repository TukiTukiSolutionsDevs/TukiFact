using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.KeyHash).IsRequired();
        builder.HasIndex(a => a.KeyHash).IsUnique();
        builder.Property(a => a.KeyPrefix).HasMaxLength(12).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(100);
        builder.Property(a => a.Permissions).HasColumnType("jsonb").HasDefaultValueSql("'[\"emit\",\"query\"]'");
        builder.Property(a => a.IsActive).HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
    }
}
