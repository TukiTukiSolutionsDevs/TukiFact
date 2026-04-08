using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class VoidedDocumentConfiguration : IEntityTypeConfiguration<VoidedDocument>
{
    public void Configure(EntityTypeBuilder<VoidedDocument> builder)
    {
        builder.ToTable("voided_documents");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(v => v.TicketType).HasMaxLength(2).IsRequired();
        builder.Property(v => v.TicketNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(v => new { v.TenantId, v.TicketNumber }).IsUnique();
        builder.Property(v => v.Status).HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(v => v.SunatTicket).HasMaxLength(50);
        builder.Property(v => v.SunatResponseCode).HasMaxLength(10);
        builder.Property(v => v.ItemsJson).HasColumnType("jsonb");
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(v => v.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(v => v.Tenant).WithMany()
            .HasForeignKey(v => v.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
