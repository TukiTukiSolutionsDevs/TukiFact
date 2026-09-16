using Dapper;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>
/// The fake module uses <c>EnsureCreated</c>, not a real EF migration (there is no production schema to migrate),
/// so this only asserts the naming convention and schema placement <c>BaseDbContext</c> is responsible for.
/// </summary>
[Collection(KernelPostgresCollection.Name)]
public sealed class BaseDbContextTests(KernelTestFixture fixture)
{
    [Fact]
    public async Task EnsureCreated_ModuleContext_CreatesSnakeCaseTableInModuleSchema()
    {
        // Arrange
        const string sql = """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = @Schema AND table_name = 'orders'
            """;
        await using var scope = fixture.Services.CreateAsyncScope();
        using var systemScope = scope.ServiceProvider.GetRequiredService<ISystemScope>().Enter("schema-inspection");
        var database = scope.ServiceProvider.GetRequiredService<IKernelTestOrderReadDbConnection>();

        // Act
        var connection = await database.OpenAsync(TestContext.Current.CancellationToken);
        var columns = await connection.QueryAsync<string>(sql, new { Schema = KernelTestDbContext.SchemaName });

        // Assert
        columns.Should().BeEquivalentTo(
            "id", "tenant_id", "reference", "created_at", "created_by", "updated_at", "updated_by", "sync_version");
    }

    [Fact]
    public async Task EnsureCreated_ModuleContext_KeepsChildTableInModuleSchema()
    {
        // Arrange
        const string sql = """
            SELECT table_schema
            FROM information_schema.tables
            WHERE table_name = 'order_lines'
            """;
        await using var scope = fixture.Services.CreateAsyncScope();
        using var systemScope = scope.ServiceProvider.GetRequiredService<ISystemScope>().Enter("schema-inspection");
        var database = scope.ServiceProvider.GetRequiredService<IKernelTestOrderReadDbConnection>();

        // Act
        var connection = await database.OpenAsync(TestContext.Current.CancellationToken);
        var schemas = await connection.QueryAsync<string>(sql);

        // Assert
        schemas.Should().Equal(KernelTestDbContext.SchemaName);
    }
}
