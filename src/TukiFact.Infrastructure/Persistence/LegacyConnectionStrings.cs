using System.Data.Common;
using Npgsql;

namespace TukiFact.Infrastructure.Persistence;

/// <summary>
/// Pool budget of the legacy <see cref="AppDbContext"/> connection string. The legacy pool
/// (<see cref="DefaultMaxPoolSize"/>) and the kernel's dedicated pool (<c>Database:KernelMaxPoolSize</c>,
/// default 20) share one Postgres server, so their sum must stay under its <c>max_connections</c>
/// (100 by default; docker-compose.prod.yml does not raise it). Npgsql's own default of 100 per pool
/// would let the legacy pool alone exhaust the server.
/// </summary>
public static class LegacyConnectionStrings
{
    public const int DefaultMaxPoolSize = 60;

    /// <summary>Npgsql's keyword and its synonym, compared without spaces and case-insensitively.</summary>
    private static readonly string[] MaxPoolSizeKeywords = ["maximumpoolsize", "maxpoolsize"];

    /// <summary>
    /// Applies <paramref name="maxPoolSize"/> as <c>Maximum Pool Size</c> unless the caller already
    /// set one (under any Npgsql spelling), in which case the connection string is returned as is.
    /// </summary>
    public static string WithPoolBudget(string connectionString, int maxPoolSize)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPoolSize, 1);

        if (DeclaresMaxPoolSize(connectionString))
        {
            return connectionString;
        }

        return new NpgsqlConnectionStringBuilder(connectionString) { MaxPoolSize = maxPoolSize }.ConnectionString;
    }

    // NpgsqlConnectionStringBuilder.ContainsKey answers "is this a valid keyword", not "was it
    // declared", so presence is read from the generic parser, which only lists declared keys.
    private static bool DeclaresMaxPoolSize(string connectionString) =>
        new DbConnectionStringBuilder { ConnectionString = connectionString }.Keys
            .Cast<string>()
            .Select(key => key.Replace(" ", string.Empty, StringComparison.Ordinal))
            .Any(key => MaxPoolSizeKeywords.Contains(key, StringComparer.OrdinalIgnoreCase));
}
