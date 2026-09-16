using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class KernelTestOrderConfiguration : IEntityTypeConfiguration<KernelTestOrder>
{
    public void Configure(EntityTypeBuilder<KernelTestOrder> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.TenantId).IsRequired();
        builder.Property(order => order.Reference).HasMaxLength(100);

        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey(line => line.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
