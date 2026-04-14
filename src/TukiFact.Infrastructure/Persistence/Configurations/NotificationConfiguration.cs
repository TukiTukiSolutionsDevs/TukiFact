using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(300).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(1000);
        builder.Property(n => n.EntityType).HasMaxLength(50);
        builder.Property(n => n.IsRead).HasDefaultValue(false);
        builder.Property(n => n.CreatedAt).HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(n => new { n.TenantId, n.IsRead, n.CreatedAt });
        builder.HasIndex(n => new { n.TenantId, n.CreatedAt });
        builder.HasIndex(n => new { n.TenantId, n.UserId });

        // Relationships
        builder.HasOne(n => n.Tenant).WithMany()
            .HasForeignKey(n => n.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
