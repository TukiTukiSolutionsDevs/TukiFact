using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.Ruc).HasMaxLength(11).IsRequired();
        builder.HasIndex(t => t.Ruc).IsUnique();
        builder.Property(t => t.RazonSocial).HasMaxLength(200).IsRequired();
        builder.Property(t => t.NombreComercial).HasMaxLength(200);
        builder.Property(t => t.Ubigeo).HasMaxLength(6);
        builder.Property(t => t.Departamento).HasMaxLength(50);
        builder.Property(t => t.Provincia).HasMaxLength(50);
        builder.Property(t => t.Distrito).HasMaxLength(50);
        builder.Property(t => t.PrimaryColor).HasMaxLength(7).HasDefaultValue("#1a73e8");
        builder.Property(t => t.SunatUser).HasMaxLength(20);
        builder.Property(t => t.GreClientId).HasMaxLength(100);
        builder.Property(t => t.GreClientSecret).HasMaxLength(200);
        builder.Property(t => t.Environment).HasMaxLength(10).HasDefaultValue("beta");
        builder.Property(t => t.IsActive).HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(t => t.Plan)
            .WithMany(p => p.Tenants)
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ApiKeys)
            .WithOne(a => a.Tenant)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
