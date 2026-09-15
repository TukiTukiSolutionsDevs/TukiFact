using FluentAssertions;
using TukiFact.Infrastructure.Persistence.Migrations;

namespace TukiFact.Api.Tests.Security;

/// <summary>
/// apply_rls_to_tenant_tables() lives in two places: docker/postgres/init/01-init.sql
/// (brand-new databases) and the FixRlsPolicyDiscovery migration (existing databases).
/// If one copy drifts, fresh and upgraded databases end up with different security
/// policies. This test pins both copies to each other so any edit to one of them
/// fails the build until the other is updated too.
/// </summary>
public class RlsFunctionParityTests
{
    private const string FunctionHeader = "CREATE OR REPLACE FUNCTION apply_rls_to_tenant_tables()";
    private const string FunctionFooter = "$$ LANGUAGE plpgsql;";

    [Fact]
    public void Init_script_and_migration_define_the_same_apply_rls_function()
    {
        var initScript = File.ReadAllText(LocateInitScript());

        var fromInitScript = Normalize(ExtractFunctionBlock(initScript));
        var fromMigration = Normalize(FixRlsPolicyDiscovery.FixedApplyRlsFunction);

        fromMigration.Should().Be(
            fromInitScript,
            "the migration body must stay byte-for-byte identical to docker/postgres/init/01-init.sql " +
            "(only line endings and trailing whitespace are ignored)");
    }

    private static string ExtractFunctionBlock(string sql)
    {
        var start = sql.IndexOf(FunctionHeader, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"the init script must define {FunctionHeader}");

        var end = sql.IndexOf(FunctionFooter, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start, $"the function must be terminated by {FunctionFooter}");

        return sql.Substring(start, end + FunctionFooter.Length - start);
    }

    /// <summary>
    /// Normalizes only what an editor may legitimately change: CRLF vs LF and trailing
    /// whitespace per line. Indentation and content are compared verbatim.
    /// </summary>
    private static string Normalize(string sql)
    {
        var lines = sql
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => line.TrimEnd());

        return string.Join("\n", lines).Trim('\n');
    }

    /// <summary>
    /// Same lookup PostgresFixture uses: walk up from the test binary directory until
    /// the repo's docker/postgres/init/01-init.sql is found.
    /// </summary>
    private static string LocateInitScript()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "docker", "postgres", "init", "01-init.sql");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            "Could not locate docker/postgres/init/01-init.sql from " + AppContext.BaseDirectory);
    }
}
