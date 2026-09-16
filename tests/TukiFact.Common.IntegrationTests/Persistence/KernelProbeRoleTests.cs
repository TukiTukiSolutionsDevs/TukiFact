using Dapper;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>
/// Proves the fixture itself is a valid RLS testbed (hard requirement: tests must run against a non-superuser,
/// non-owner role) — otherwise every isolation assertion in this collection would pass vacuously because a
/// superuser or table owner bypasses row level security regardless of any policy.
/// </summary>
[Collection(KernelPostgresCollection.Name)]
public sealed class KernelProbeRoleTests(KernelTestFixture fixture)
{
    [Fact]
    public async Task Kernel_connections_run_as_the_non_superuser_probe_role()
    {
        // Arrange
        await using var scope = fixture.Services.CreateAsyncScope();
        using var systemScope = scope.ServiceProvider.GetRequiredService<ISystemScope>().Enter("probe-role-check");
        var database = scope.ServiceProvider.GetRequiredService<IKernelTestOrderReadDbConnection>();
        var connection = await database.OpenAsync(TestContext.Current.CancellationToken);

        // Act
        var currentUser = await connection.QuerySingleAsync<string>("SELECT current_user");
        var isSuperuser = await connection.QuerySingleAsync<bool>(
            "SELECT rolsuper FROM pg_roles WHERE rolname = current_user");
        var isTableOwner = await connection.QuerySingleAsync<bool>(
            "SELECT pg_has_role(current_user, (SELECT tableowner FROM pg_tables WHERE schemaname = 'kernel_test' AND tablename = 'orders'), 'MEMBER')");

        // Assert
        currentUser.Should().Be("kernel_probe");
        isSuperuser.Should().BeFalse("a superuser bypasses row level security regardless of policy");
        isTableOwner.Should().BeFalse("a table owner bypasses row level security regardless of policy");
    }
}
