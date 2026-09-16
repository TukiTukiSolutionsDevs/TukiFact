using Microsoft.Extensions.DependencyInjection;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Time;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Time;

public sealed class ClockTests
{
    private static readonly DateTimeOffset UtcInstant = new(2026, 9, 14, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UtcNow_FixedTimeProvider_ReturnsProviderInstant()
    {
        // Arrange
        using var provider = ApplicationTestHost.Build(services =>
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(UtcInstant)));
        var clock = provider.GetRequiredService<IClock>();

        // Act
        var utcNow = clock.UtcNow;

        // Assert
        utcNow.Should().Be(UtcInstant);
        utcNow.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void LimaNow_UtcInstant_ReturnsSameInstantOnLimaWallClock()
    {
        // Arrange
        using var provider = ApplicationTestHost.Build(services =>
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(UtcInstant)));
        var clock = provider.GetRequiredService<IClock>();

        // Act
        var limaNow = clock.LimaNow;

        // Assert
        limaNow.Should().Be(UtcInstant);
        limaNow.Offset.Should().Be(TimeSpan.FromHours(-5));
        limaNow.Hour.Should().Be(10);
    }

    [Fact]
    public void LimaNow_SystemTimeZoneUnavailable_FallsBackToFixedMinusFiveOffset()
    {
        // Arrange
        var clock = new Clock(new FixedTimeProvider(UtcInstant), () => throw new TimeZoneNotFoundException());

        // Act
        var limaNow = clock.LimaNow;

        // Assert
        limaNow.Should().Be(UtcInstant);
        limaNow.Offset.Should().Be(TimeSpan.FromHours(-5));
        limaNow.Hour.Should().Be(10);
    }
}
