using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.DependencyInjection;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.Infrastructure.Modules;
using TukiFact.Common.Infrastructure.Persistence;
using TukiFact.TestSupport.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace TukiFact.TestSupport.Modules;

/// <summary>
/// Collection fixture of a module's integration tests: one Postgres container, the Common registrations plus
/// <see cref="IModule.Register"/> as the host runs them, and the module's schema created once. Unlike the legacy
/// stack, the service provider tests exercise connects as a dedicated, non-superuser, non-owner Postgres role
/// (<see cref="ProbeRoleName"/>) so row level security actually applies — schema creation and role/RLS setup run
/// separately, as the container's owner (superuser) role, before that provider is built.
/// </summary>
/// <example>
/// <code>
/// public sealed class KernelProbeFixture : KernelIntegrationFixture
/// {
///     protected override IModule Module { get; } = new KernelProbeModule();
///     protected override string Schema => KernelTestDbContext.SchemaName;
///     protected override Task ApplyRowLevelSecurityAsync(NpgsqlConnection ownerConnection) => ...;
/// }
/// </code>
/// </example>
public abstract class KernelIntegrationFixture : IAsyncLifetime
{
    protected const string ProbeRoleName = "kernel_probe";
    protected const string ProbeRolePassword = "kernel_probe_test_2026";

    private readonly PostgresFixture _postgres = new();
    private ServiceProvider? _services;

    public IServiceProvider Services => _services ?? throw new InvalidOperationException("Fixture is not initialized.");

    /// <summary>The module under test, registered as the host registers it.</summary>
    protected abstract IModule Module { get; }

    /// <summary>Postgres schema owned by <see cref="Module"/>.</summary>
    protected abstract string Schema { get; }

    public async ValueTask InitializeAsync()
    {
        await _postgres.InitializeAsync();

        var ownerServices = BuildServiceCollection(_postgres.ConnectionString);
        ConfigureTestServices(ownerServices);
        await using (var ownerProvider = ownerServices.BuildServiceProvider())
        {
            foreach (var contextType in DiscoverDbContextTypes(ownerServices))
            {
                // Module contexts refuse to open a connection outside a kernel transaction, so schema creation
                // runs inside one, under the audited system scope (there is no tenant yet).
                await using var scope = ownerProvider.CreateAsyncScope();
                using var systemScope = scope.ServiceProvider.GetRequiredService<ISystemScope>().Enter("schema-creation");
                var transactions = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
                await transactions.BeginTransactionAsync();
                await ((DbContext)scope.ServiceProvider.GetRequiredService(contextType)).Database.EnsureCreatedAsync();
                await transactions.CommitAsync();
            }
        }

        await using (var ownerConnection = new NpgsqlConnection(_postgres.ConnectionString))
        {
            await ownerConnection.OpenAsync();
            await CreateProbeRoleAsync(ownerConnection);
            await ApplyRowLevelSecurityAsync(ownerConnection);
        }

        var probeServices = BuildServiceCollection(BuildProbeConnectionString());
        ConfigureTestServices(probeServices);

        _services = probeServices.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Sends a command or query in its own scope, as one HTTP request would.</summary>
    /// <param name="request">The command or query.</param>
    /// <param name="configureScope">Arranges scoped test doubles (e.g. the current user) before sending.</param>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, Action<IServiceProvider>? configureScope = null)
        where TResponse : Result
    {
        await using var scope = Services.CreateAsyncScope();
        configureScope?.Invoke(scope.ServiceProvider);

        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Host configuration: the collection database. Override to add module settings.</summary>
    protected virtual Dictionary<string, string?> Settings(string databaseConnectionString) =>
        new() { ["ConnectionStrings:Database"] = databaseConnectionString };

    /// <summary>
    /// Test doubles, registered after Common and the module. Use <c>Replace</c> for services Common adds with
    /// <c>TryAdd</c> (e.g. <see cref="TimeProvider"/>).
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // No test doubles by default: the module runs with its real registrations.
    }

    /// <summary>
    /// Owner-privileged: grants the non-superuser probe role access to <see cref="Schema"/> and enables/forces row
    /// level security with the tenant isolation policy the module needs. Runs once, after schema creation.
    /// </summary>
    protected abstract Task ApplyRowLevelSecurityAsync(NpgsqlConnection ownerConnection);

    private static async Task CreateProbeRoleAsync(NpgsqlConnection ownerConnection)
    {
        var sql = $"""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{ProbeRoleName}') THEN
                    CREATE ROLE {ProbeRoleName} LOGIN PASSWORD '{ProbeRolePassword}' NOSUPERUSER NOCREATEDB NOCREATEROLE;
                END IF;
            END
            $$;
            """;

        await using var command = new NpgsqlCommand(sql, ownerConnection);
        await command.ExecuteNonQueryAsync();
    }

    private string BuildProbeConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(_postgres.ConnectionString)
        {
            Username = ProbeRoleName,
            Password = ProbeRolePassword,
            IncludeErrorDetail = true,
        };

        return builder.ConnectionString;
    }

    private ServiceCollection BuildServiceCollection(string connectionString)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(Settings(connectionString)).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommonApplication();
        services.AddCommonPersistence(configuration);
        Module.Register(services, configuration);
        return services;
    }

    private static IEnumerable<Type> DiscoverDbContextTypes(IServiceCollection services) =>
        [.. services
            .Select(descriptor => descriptor.ServiceType)
            .Where(serviceType => serviceType.IsAssignableTo(typeof(BaseDbContext)))
            .Distinct()];
}
