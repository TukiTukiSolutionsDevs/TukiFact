using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class TenantServiceConfigConfiguration : IEntityTypeConfiguration<TenantServiceConfig>
{
    public void Configure(EntityTypeBuilder<TenantServiceConfig> builder)
    {
        builder.ToTable("tenant_service_configs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(x => x.TenantId).IsUnique(); // one config per tenant

        builder.Property(x => x.LookupProvider).HasMaxLength(30).HasDefaultValue("none");
        builder.Property(x => x.LookupApiKey).HasMaxLength(500);

        builder.Property(x => x.AiProvider).HasMaxLength(30).HasDefaultValue("none");
        builder.Property(x => x.AiApiKey).HasMaxLength(500);
        builder.Property(x => x.AiModel).HasMaxLength(100);

        // Email config
        builder.Property(x => x.AutoSendEmail).HasDefaultValue(false);
        builder.Property(x => x.EmailProvider).HasMaxLength(20).HasDefaultValue("log");
        builder.Property(x => x.ResendApiKey).HasMaxLength(500);
        builder.Property(x => x.SmtpHost).HasMaxLength(200);
        builder.Property(x => x.SmtpUser).HasMaxLength(200);
        builder.Property(x => x.SmtpPassword).HasMaxLength(500);
        builder.Property(x => x.EmailFromName).HasMaxLength(100);
        builder.Property(x => x.EmailFromAddress).HasMaxLength(255);

        builder.HasOne(x => x.Tenant)
            .WithOne(t => t.ServiceConfig)
            .HasForeignKey<TenantServiceConfig>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
