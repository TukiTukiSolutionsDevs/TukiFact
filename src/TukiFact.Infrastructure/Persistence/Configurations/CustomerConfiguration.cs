using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.DocType).HasMaxLength(2).IsRequired();
        builder.Property(c => c.DocNumber).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Ubigeo).HasMaxLength(6);
        builder.Property(c => c.Departamento).HasMaxLength(100);
        builder.Property(c => c.Provincia).HasMaxLength(100);
        builder.Property(c => c.Distrito).HasMaxLength(100);
        builder.Property(c => c.Category).HasMaxLength(50);
        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.Property(c => c.IsActive).HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(c => new { c.TenantId, c.DocNumber }).IsUnique();

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
