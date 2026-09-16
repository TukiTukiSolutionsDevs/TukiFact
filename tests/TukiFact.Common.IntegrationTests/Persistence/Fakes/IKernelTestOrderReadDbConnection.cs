using System.Data.Common;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

public interface IKernelTestOrderReadDbConnection
{
    Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default);
}
