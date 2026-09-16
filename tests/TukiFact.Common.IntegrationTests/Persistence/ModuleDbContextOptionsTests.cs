using TukiFact.Common.Infrastructure.Persistence;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>Provider options contract shared by runtime and design-time factories; no database is contacted.</summary>
public sealed class ModuleDbContextOptionsTests
{
    [Fact]
    public void Configure_ConnectionOverload_PlacesMigrationsHistoryInModuleSchema()
    {
        // Arrange
        var builder = new DbContextOptionsBuilder<KernelTestDbContext>();
        using var connection = new NpgsqlConnection("Host=localhost;Database=unused");

        // Act
        ModuleDbContextOptions.Configure(builder, connection, KernelTestDbContext.SchemaName);

        // Assert
        var relational = RelationalOptionsExtension.Extract(builder.Options);
        relational.MigrationsHistoryTableName.Should().Be(ModuleDbContextOptions.MigrationsHistoryTable);
        relational.MigrationsHistoryTableSchema.Should().Be(KernelTestDbContext.SchemaName);
    }

    [Fact]
    public void Configure_ConnectionStringOverload_PlacesMigrationsHistoryInModuleSchema()
    {
        // Arrange
        var builder = new DbContextOptionsBuilder<KernelTestDbContext>();

        // Act
        ModuleDbContextOptions.Configure(builder, "Host=localhost;Database=unused", KernelTestDbContext.SchemaName);

        // Assert
        var relational = RelationalOptionsExtension.Extract(builder.Options);
        relational.MigrationsHistoryTableName.Should().Be(ModuleDbContextOptions.MigrationsHistoryTable);
        relational.MigrationsHistoryTableSchema.Should().Be(KernelTestDbContext.SchemaName);
    }
}
