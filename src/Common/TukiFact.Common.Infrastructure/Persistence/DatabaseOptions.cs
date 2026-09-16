using System.ComponentModel.DataAnnotations;

namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>Single Postgres connection for every kernel module (<c>ConnectionStrings:Database</c>, section <c>Database</c>).</summary>
internal sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "Database";

    [Required(ErrorMessage = "ConnectionStrings:Database is required.")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// <c>Maximum Pool Size</c> of the kernel's dedicated <see cref="Npgsql.NpgsqlDataSource"/> (ADR-008). The kernel pool
    /// and the legacy <c>AppDbContext</c> pool are separate physical pools on the same server, so their sum (plus the
    /// Postgres reserved slots) must stay under the server's <c>max_connections</c>.
    /// </summary>
    [Range(1, 1_000)]
    public int KernelMaxPoolSize { get; set; } = 20;
}
