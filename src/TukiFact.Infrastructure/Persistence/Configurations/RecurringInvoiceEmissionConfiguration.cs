using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class RecurringInvoiceEmissionConfiguration : IEntityTypeConfiguration<RecurringInvoiceEmission>
{
    public void Configure(EntityTypeBuilder<RecurringInvoiceEmission> builder)
    {
        builder.ToTable("recurring_invoice_emissions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.SunatResponseCode).HasMaxLength(20);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        // Idempotency: a single (schedule, date) pair can only ever exist once.
        builder.HasIndex(e => new { e.RecurringInvoiceId, e.TargetDate }).IsUnique();

        builder.HasOne(e => e.RecurringInvoice).WithMany()
            .HasForeignKey(e => e.RecurringInvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}
