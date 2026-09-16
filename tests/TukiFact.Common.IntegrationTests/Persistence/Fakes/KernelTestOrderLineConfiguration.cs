using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class KernelTestOrderLineConfiguration : IEntityTypeConfiguration<KernelTestOrderLine>
{
    public void Configure(EntityTypeBuilder<KernelTestOrderLine> builder)
    {
        builder.ToTable("order_lines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();
        builder.Property(line => line.Description).HasMaxLength(200);
    }
}
