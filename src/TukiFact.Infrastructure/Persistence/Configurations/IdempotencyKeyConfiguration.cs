using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Key).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Endpoint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ResponseBody).HasColumnType("text").IsRequired();

        // Same (tenant + key) replayed → same row.
        builder.HasIndex(x => new { x.TenantId, x.Key }).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
    }
}
