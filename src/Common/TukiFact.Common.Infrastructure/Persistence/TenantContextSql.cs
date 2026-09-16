using Npgsql;

namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// The only SQL that touches the RLS GUCs (ADR-003/ADR-008). Both statements are parameterised via
/// <c>set_config</c>; the <c>is_local</c> flag decides whether the value dies with the transaction
/// (<see cref="TransactionManager"/>) or with the session (<see cref="ReadDbConnection"/>, reset on dispose).
/// </summary>
internal static class TenantContextSql
{
    public const string TenantGuc = "app.current_tenant";
    public const string BypassGuc = "app.bypass_rls";

    private const string TenantParameter = "tenant";
    private const string BypassParameter = "bypass";

    private const string SetTransactionLocal =
        $"SELECT set_config('{TenantGuc}', @{TenantParameter}, true), set_config('{BypassGuc}', @{BypassParameter}, true)";

    private const string SetSession =
        $"SELECT set_config('{TenantGuc}', @{TenantParameter}, false), set_config('{BypassGuc}', @{BypassParameter}, false)";

    public const string ResetSession = $"SELECT set_config('{TenantGuc}', '', false), set_config('{BypassGuc}', 'off', false)";

    public static NpgsqlCommand CreateSetCommand(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid? tenantId,
        bool bypass,
        bool transactionLocal)
    {
        var command = new NpgsqlCommand(transactionLocal ? SetTransactionLocal : SetSession, connection, transaction);
        command.Parameters.AddWithValue(TenantParameter, tenantId?.ToString() ?? string.Empty);
        command.Parameters.AddWithValue(BypassParameter, bypass ? "on" : "off");
        return command;
    }
}
