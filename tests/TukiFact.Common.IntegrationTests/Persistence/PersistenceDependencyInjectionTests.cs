using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.DependencyInjection;
using TukiFact.Common.Infrastructure.Persistence;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Globalization;

namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>Registration contract; no database is contacted.</summary>
public sealed class PersistenceDependencyInjectionTests
{
    [Fact]
    public async Task AddCommonPersistence_MissingDatabaseConnectionString_FailsOptionsValidation()
    {
        // Arrange
        await using var provider = BuildProvider(connectionString: null);

        // Act
        var act = () => provider.GetRequiredService<ISqlConnectionFactory>();

        // Assert
        act.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain("ConnectionStrings:Database");
    }

    [Fact]
    public async Task AddModuleDbContext_Registered_DoesNotRetryOnFailureSoExplicitTransactionsAreAllowed()
    {
        // Arrange
        await using var provider = BuildProvider("Host=localhost;Database=unused");
        await using var scope = provider.CreateAsyncScope();

        // Act
        var strategy = scope.ServiceProvider.GetRequiredService<KernelTestDbContext>().Database.CreateExecutionStrategy();

        // Assert
        strategy.RetriesOnFailure.Should().BeFalse();
    }

    [Fact]
    public async Task AddCommonPersistence_DefaultOptions_CapsKernelPoolAtTwenty()
    {
        // Arrange
        await using var provider = BuildProvider("Host=localhost;Database=unused");

        // Act
        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();

        // Assert
        dataSource.ConnectionString.Should().Contain("Maximum Pool Size=20");
    }

    [Fact]
    public async Task AddCommonPersistence_ConfiguredKernelMaxPoolSize_AppliesItToKernelDataSource()
    {
        // Arrange
        await using var provider = BuildProvider("Host=localhost;Database=unused", kernelMaxPoolSize: 7);

        // Act
        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();

        // Assert
        dataSource.ConnectionString.Should().Contain("Maximum Pool Size=7");
    }

    private static ServiceProvider BuildProvider(string? connectionString, int? kernelMaxPoolSize = null)
    {
        var settings = new Dictionary<string, string?> { ["ConnectionStrings:Database"] = connectionString };
        if (kernelMaxPoolSize is not null)
        {
            settings["Database:KernelMaxPoolSize"] = kernelMaxPoolSize.Value.ToString(CultureInfo.InvariantCulture);
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ICurrentUser, TestCurrentUser>();
        services.AddCommonApplication(typeof(PersistenceDependencyInjectionTests).Assembly);
        services.AddCommonPersistence(configuration);
        services.AddModuleDbContext<KernelTestDbContext>(KernelTestDbContext.SchemaName);

        return services.BuildServiceProvider();
    }
}
