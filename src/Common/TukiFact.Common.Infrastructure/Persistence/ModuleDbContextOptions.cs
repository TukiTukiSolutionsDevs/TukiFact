using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// Provider options shared by runtime registration and design-time factories, so migrations are generated
/// against the same model the application runs.
/// </summary>
public static class ModuleDbContextOptions
{
    /// <summary>EF default name, kept inside each module schema so modules migrate independently.</summary>
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, DbConnection connection, string schema)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        return options
            .UseNpgsql(connection, npgsql => ConfigureNpgsql(npgsql, schema))
            .UseSnakeCaseNamingConvention();
    }

    /// <summary>Design-time overload for <c>IDesignTimeDbContextFactory</c>; dotnet-ef does not open the connection to add migrations.</summary>
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString, string schema)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        return options
            .UseNpgsql(connectionString, npgsql => ConfigureNpgsql(npgsql, schema))
            .UseSnakeCaseNamingConvention();
    }

    // Deliberately no EnableRetryOnFailure: TransactionBehavior opens explicit transactions, which
    // NpgsqlRetryingExecutionStrategy rejects, and replaying a whole command would re-run domain event handlers.
    // Transient failures surface as exceptions; retries belong to idempotent callers, not the kernel.
    private static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql, string schema) =>
        npgsql.MigrationsHistoryTable(MigrationsHistoryTable, schema);
}
