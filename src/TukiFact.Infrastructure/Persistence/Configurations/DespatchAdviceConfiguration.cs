using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class DespatchAdviceConfiguration : IEntityTypeConfiguration<DespatchAdvice>
{
    public void Configure(EntityTypeBuilder<DespatchAdvice> builder)
    {
        builder.ToTable("despatch_advices");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.DocumentType).HasMaxLength(2).IsRequired();
        builder.Property(d => d.Serie).HasMaxLength(4).IsRequired();
        builder.Property(d => d.Correlative).IsRequired();
        builder.Ignore(d => d.FullNumber); // Computed, not stored

        builder.Property(d => d.IssueDate).IsRequired();
        builder.Property(d => d.IssueTime).IsRequired();
        builder.Property(d => d.TransferStartDate).IsRequired();

        // Transfer reason
        builder.Property(d => d.TransferReasonCode).HasMaxLength(2).IsRequired();
        builder.Property(d => d.TransferReasonDescription).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Note).HasMaxLength(500);

        // Weight
        builder.Property(d => d.GrossWeight).HasPrecision(14, 2).IsRequired();
        builder.Property(d => d.WeightUnitCode).HasMaxLength(3).HasDefaultValue("KGM");

        // Transport
        builder.Property(d => d.TransportMode).HasMaxLength(2).IsRequired();

        // Carrier
        builder.Property(d => d.CarrierDocType).HasMaxLength(1);
        builder.Property(d => d.CarrierDocNumber).HasMaxLength(15);
        builder.Property(d => d.CarrierName).HasMaxLength(200);
        builder.Property(d => d.CarrierMtcNumber).HasMaxLength(20);

        // Driver
        builder.Property(d => d.DriverDocType).HasMaxLength(1);
        builder.Property(d => d.DriverDocNumber).HasMaxLength(15);
        builder.Property(d => d.DriverName).HasMaxLength(200);
        builder.Property(d => d.DriverLicense).HasMaxLength(20);

        // Vehicle
        builder.Property(d => d.VehiclePlate).HasMaxLength(10);
        builder.Property(d => d.SecondaryVehiclePlate).HasMaxLength(10);

        // Recipient
        builder.Property(d => d.RecipientDocType).HasMaxLength(1).IsRequired();
        builder.Property(d => d.RecipientDocNumber).HasMaxLength(15).IsRequired();
        builder.Property(d => d.RecipientName).HasMaxLength(200).IsRequired();

        // Addresses
        builder.Property(d => d.OriginUbigeo).HasMaxLength(6).IsRequired();
        builder.Property(d => d.OriginAddress).HasMaxLength(300).IsRequired();
        builder.Property(d => d.DestinationUbigeo).HasMaxLength(6).IsRequired();
        builder.Property(d => d.DestinationAddress).HasMaxLength(300).IsRequired();

        // Related document
        builder.Property(d => d.RelatedDocType).HasMaxLength(2);
        builder.Property(d => d.RelatedDocNumber).HasMaxLength(20);

        // SUNAT
        builder.Property(d => d.Status).HasMaxLength(20).HasDefaultValue("draft");
        builder.Property(d => d.SunatResponseCode).HasMaxLength(10);
        builder.Property(d => d.SunatResponseMessage).HasMaxLength(500);
        builder.Property(d => d.SunatTicket).HasMaxLength(50);

        // Files
        builder.Property(d => d.XmlUrl).HasMaxLength(500);
        builder.Property(d => d.PdfUrl).HasMaxLength(500);
        builder.Property(d => d.CdrUrl).HasMaxLength(500);

        // Audit
        builder.Property(d => d.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(d => d.UpdatedAt).HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(d => new { d.TenantId, d.Serie, d.Correlative }).IsUnique();
        builder.HasIndex(d => new { d.TenantId, d.Status });
        builder.HasIndex(d => new { d.TenantId, d.IssueDate });

        // Relationships
        builder.HasOne(d => d.Tenant).WithMany()
            .HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Items).WithOne(i => i.DespatchAdvice)
            .HasForeignKey(i => i.DespatchAdviceId).OnDelete(DeleteBehavior.Cascade);
    }
}
