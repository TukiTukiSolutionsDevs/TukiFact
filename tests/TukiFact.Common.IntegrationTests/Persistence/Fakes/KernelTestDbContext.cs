using TukiFact.Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>Test module context: owns the <c>kernel_test</c> schema, created via <c>EnsureCreated</c> (no
/// production migrations exist for a schema that is never shipped).</summary>
public sealed class KernelTestDbContext(DbContextOptions<KernelTestDbContext> options) : BaseDbContext(options)
{
    public const string SchemaName = "kernel_test";

    internal DbSet<KernelTestOrder> Orders => Set<KernelTestOrder>();

    protected override string Schema => SchemaName;
}
