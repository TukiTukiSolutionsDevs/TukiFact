using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class WebhookConfigConfiguration : IEntityTypeConfiguration<WebhookConfig>
{
    public void Configure(EntityTypeBuilder<WebhookConfig> builder)
    {
        builder.ToTable("webhook_configs");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(w => w.Url).HasMaxLength(500).IsRequired();
        builder.Property(w => w.Secret).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Events).HasColumnType("jsonb").HasDefaultValueSql("'[]'");
        builder.Property(w => w.IsActive).HasDefaultValue(true);
        builder.Property(w => w.MaxRetries).HasDefaultValue(3);
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("now()");
        builder.HasOne(w => w.Tenant).WithMany().HasForeignKey(w => w.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
