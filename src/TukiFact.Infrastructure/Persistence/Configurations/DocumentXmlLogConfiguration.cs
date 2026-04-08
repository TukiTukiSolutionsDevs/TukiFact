using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class DocumentXmlLogConfiguration : IEntityTypeConfiguration<DocumentXmlLog>
{
    public void Configure(EntityTypeBuilder<DocumentXmlLog> builder)
    {
        builder.ToTable("document_xml_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(l => l.Action).HasMaxLength(30).IsRequired();
        builder.Property(l => l.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(l => l.DocumentId);
    }
}
