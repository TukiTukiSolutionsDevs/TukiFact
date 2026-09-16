using System.Data.Common;

namespace TukiFact.Common.Application.Abstractions;

/// <summary>Opens connections for the read side; each module specializes it as <c>I&lt;Module&gt;ReadDbConnection</c>.</summary>
public interface ISqlConnectionFactory
{
    /// <summary>Creates a closed connection; Dapper opens and closes it around each call.</summary>
    DbConnection CreateConnection();

    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
