using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD").IsRequired();
        builder.Property(e => e.BuyRate).HasPrecision(10, 4).IsRequired();
        builder.Property(e => e.SellRate).HasPrecision(10, 4).IsRequired();
        builder.Property(e => e.Source).HasMaxLength(20).HasDefaultValue("SBS").IsRequired();
        builder.Property(e => e.FetchedAt).HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.Date, e.Currency }).IsUnique();
    }
}
