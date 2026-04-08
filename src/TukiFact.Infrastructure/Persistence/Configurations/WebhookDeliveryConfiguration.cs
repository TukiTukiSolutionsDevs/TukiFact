using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_deliveries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.EventType).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Payload).HasColumnType("jsonb");
        builder.Property(d => d.ResponseStatus).HasMaxLength(10);
        builder.Property(d => d.Status).HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(d => d.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(d => d.WebhookConfigId);
        builder.HasOne(d => d.WebhookConfig).WithMany().HasForeignKey(d => d.WebhookConfigId).OnDelete(DeleteBehavior.Cascade);
    }
}
