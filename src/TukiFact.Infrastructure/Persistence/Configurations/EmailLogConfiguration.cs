using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.To).HasMaxLength(255).IsRequired();
        builder.Property(e => e.Cc).HasMaxLength(255);
        builder.Property(e => e.Subject).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Template).HasMaxLength(50).HasDefaultValue("generic");

        builder.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.Property(e => e.ExternalId).HasMaxLength(100);
        builder.Property(e => e.Provider).HasMaxLength(20).HasDefaultValue("log");

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => new { e.TenantId, e.CreatedAt });
        builder.HasIndex(e => e.DocumentId);

        // Relationships
        builder.HasOne(e => e.Tenant).WithMany()
            .HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
