using Npgsql;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Tests.Persistence;

/// <summary>
/// <see cref="LegacyConnectionStrings.WithPoolBudget"/> caps the legacy pool only when the caller
/// has not chosen a size: an explicit <c>Maximum Pool Size</c> (under any Npgsql spelling) wins.
/// </summary>
public sealed class LegacyConnectionStringsTests
{
    private const string BaseConnectionString = "Host=localhost;Database=tukifact;Username=tukifact;Password=secret";

    [Fact]
    public void WithPoolBudget_NoPoolSizeSet_AppliesTheBudget()
    {
        // Act
        var result = LegacyConnectionStrings.WithPoolBudget(BaseConnectionString, 60);

        // Assert
        Assert.Equal(60, new NpgsqlConnectionStringBuilder(result).MaxPoolSize);
    }

    [Fact]
    public void WithPoolBudget_NoPoolSizeSet_KeepsTheOtherSettings()
    {
        // Act
        var result = LegacyConnectionStrings.WithPoolBudget(BaseConnectionString, 60);

        // Assert
        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("localhost", builder.Host);
        Assert.Equal("tukifact", builder.Database);
        Assert.Equal("tukifact", builder.Username);
        Assert.Equal("secret", builder.Password);
    }

    [Theory]
    [InlineData("Maximum Pool Size=15")]
    [InlineData("MaxPoolSize=15")]
    public void WithPoolBudget_PoolSizeAlreadySet_KeepsTheCallerValue(string poolSetting)
    {
        // Arrange
        var connectionString = $"{BaseConnectionString};{poolSetting}";

        // Act
        var result = LegacyConnectionStrings.WithPoolBudget(connectionString, 60);

        // Assert
        Assert.Equal(15, new NpgsqlConnectionStringBuilder(result).MaxPoolSize);
    }

    [Fact]
    public void WithPoolBudget_NonPositiveBudget_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => LegacyConnectionStrings.WithPoolBudget(BaseConnectionString, 0));
    }
}
