using System.Data.Common;
using TukiFact.Common.Application.Abstractions;
using Npgsql;

namespace TukiFact.Common.Infrastructure.Persistence;

internal sealed class NpgsqlSqlConnectionFactory(NpgsqlDataSource dataSource) : ISqlConnectionFactory
{
    public DbConnection CreateConnection() => dataSource.CreateConnection();

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
        await dataSource.OpenConnectionAsync(cancellationToken);
}
