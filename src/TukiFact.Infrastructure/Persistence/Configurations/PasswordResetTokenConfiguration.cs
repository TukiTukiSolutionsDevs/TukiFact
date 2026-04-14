using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.Token).HasMaxLength(128).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        builder.Ignore(t => t.IsUsed);
        builder.Ignore(t => t.IsExpired);

        // Indexes
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.UserId);

        // Relationships
        builder.HasOne(t => t.User).WithMany()
            .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
