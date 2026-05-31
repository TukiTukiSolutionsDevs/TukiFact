using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.PlanId).IsRequired();

        builder.Property(s => s.Status).HasMaxLength(20).HasDefaultValue("active").IsRequired();
        builder.Property(s => s.StartDate).HasDefaultValueSql("now()");
        builder.Property(s => s.NextBillingDate).IsRequired();
        builder.Property(s => s.MonthlyAmount).HasPrecision(10, 2);
        builder.Property(s => s.DocumentsUsedThisMonth).HasDefaultValue(0);
        builder.Property(s => s.DocumentsLimit).IsRequired();

        builder.Property(s => s.CulqiCustomerId).HasMaxLength(80);
        builder.Property(s => s.CulqiCardId).HasMaxLength(80);
        builder.Property(s => s.CulqiSubscriptionId).HasMaxLength(80);
        builder.Property(s => s.LastChargeId).HasMaxLength(80);
        builder.Property(s => s.CancellationReason).HasMaxLength(200);

        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");

        // A tenant has at most one active subscription. Status=cancelled rows stay for audit
        // but only one active|past_due|trial row is allowed at a time (enforced in service layer).
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.CulqiSubscriptionId)
            .IsUnique()
            .HasFilter("\"CulqiSubscriptionId\" IS NOT NULL");
        builder.HasIndex(s => new { s.Status, s.NextBillingDate });

        builder.HasOne(s => s.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
