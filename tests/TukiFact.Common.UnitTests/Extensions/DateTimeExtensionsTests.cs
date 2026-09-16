using TukiFact.Common.Domain.Extensions;

namespace TukiFact.Common.UnitTests.Extensions;

public sealed class DateTimeExtensionsTests
{
    [Fact]
    public void ToUnixTimestamp_UnixEpoch_ReturnsZero()
    {
        // Act & Assert
        DateTime.UnixEpoch.ToUnixTimestamp().Should().Be(0L);
    }

    [Fact]
    public void ToUnixTimestamp_DateAfter2038_ReturnsSecondsBeyondInt32Range()
    {
        // Arrange
        var dateTime = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var timestamp = dateTime.ToUnixTimestamp();

        // Assert
        timestamp.Should().Be(4102444800L);
        timestamp.Should().BeGreaterThan(int.MaxValue);
    }
}
