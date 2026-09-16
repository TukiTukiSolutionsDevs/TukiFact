using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Infrastructure.Messaging.Correlation;
using TukiFact.Common.Infrastructure.Persistence.Interceptors;
using TukiFact.Common.Infrastructure.Persistence.TypeHandlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace TukiFact.Common.Infrastructure.Persistence;

public static class PersistenceDependencyInjection
{
    /// <summary>Npgsql identifies the kernel's connections with this name (ADR-008), so its pool is never the
    /// same physical pool as the legacy <c>AppDbContext</c> connections — a session-level GUC set by
    /// <see cref="ReadDbConnection"/> can therefore never bleed into a legacy connection, and vice versa.</summary>
    internal const string KernelApplicationName = "tukifact-kernel";

    /// <summary>
    /// Registers what every module shares: the validated <c>ConnectionStrings:Database</c>, one dedicated
    /// <see cref="NpgsqlDataSource"/> (ADR-008), the read-side <see cref="ISqlConnectionFactory"/>, the scoped
    /// <see cref="ITransactionManager"/>, the audited <see cref="ISystemScope"/> and the EF interceptors.
    /// Call once from the host.
    /// </summary>
    public static IServiceCollection AddCommonPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Configure(options => options.ConnectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var kernelConnectionString = new NpgsqlConnectionStringBuilder(options.ConnectionString)
            {
                ApplicationName = KernelApplicationName,
                MaxPoolSize = options.KernelMaxPoolSize,
            };
            return new NpgsqlDataSourceBuilder(kernelConnectionString.ConnectionString).Build();
        });
        services.TryAddSingleton<ISqlConnectionFactory, NpgsqlSqlConnectionFactory>();
        DapperTypeHandlers.Register();

        services.TryAddScoped<TransactionManager>();
        services.TryAddScoped<ITransactionManager>(provider => provider.GetRequiredService<TransactionManager>());
        services.TryAddScoped<ISystemScope, SystemScope>();
        // Authentication/ and Messaging/Correlation/ ship in this assembly because the RLS hook itself needs them:
        // the tenant GUC comes from ICurrentUser and every fail-closed/audit log line carries ICorrelationIdAccessor.
        services.TryAddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

        services.TryAddScoped<TransactionRequiredConnectionInterceptor>();
        services.TryAddScoped<DomainEventsInterceptor>();
        services.TryAddScoped<AuditableInterceptor>();
        services.TryAddSingleton<SyncVersionInterceptor>();

        return services;
    }

    /// <summary>HTTP hosts only: maps <see cref="DbUpdateConcurrencyException"/> to a 409 ProblemDetails.</summary>
    public static IServiceCollection AddPersistenceExceptionHandling(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddExceptionHandler<DbUpdateConcurrencyExceptionHandler>();
        return services;
    }

    /// <summary>
    /// Registers a module context on the shared connection with its own <paramref name="schema"/> (default schema
    /// and migrations history), the domain events → auditable → sync_version interceptors, in that order, and the
    /// connection guard that rejects any use of the context outside a kernel transaction.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string schema)
        where TContext : BaseDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        services.AddDbContext<TContext>((provider, options) =>
        {
            ModuleDbContextOptions.Configure(options, provider.GetRequiredService<TransactionManager>().Connection, schema);
            options.AddInterceptors(
                provider.GetRequiredService<DomainEventsInterceptor>(),
                provider.GetRequiredService<AuditableInterceptor>(),
                provider.GetRequiredService<SyncVersionInterceptor>(),
                provider.GetRequiredService<TransactionRequiredConnectionInterceptor>());
        });

        // Replace the default activation so each context enlists in the scope's shared connection and transaction.
        services.Replace(ServiceDescriptor.Scoped(provider =>
        {
            var context = ActivatorUtilities.CreateInstance<TContext>(provider);
            provider.GetRequiredService<TransactionManager>().Enlist(context);
            return context;
        }));

        return services;
    }

    /// <summary>Registers a module read port (<c>I&lt;Module&gt;ReadDbConnection</c>) backed by <see cref="ReadDbConnection"/>.</summary>
    public static IServiceCollection AddReadDbConnection<TConnection, TImplementation>(this IServiceCollection services)
        where TConnection : class
        where TImplementation : ReadDbConnection, TConnection
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<TConnection, TImplementation>();
        return services;
    }
}
