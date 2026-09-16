using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.UnitTests.Behaviors.Fakes;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Behaviors;

public sealed class LoggingBehaviorTests
{
    [Fact]
    public async Task Send_CommandHandlerThrows_LogsCrashAtErrorLevelAndRethrows()
    {
        // Arrange
        await using var provider = BuildCapturingHost();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var act = () => sender.Send(new CrashOrderCommand(), TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;
        var crash = provider.GetRequiredService<LogSink>().Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Error).Which;
        crash.Exception.Should().BeSameAs(exception);
        crash.Message.Should().Contain("CrashOrder");
    }

    [Fact]
    public async Task Send_CommandWithSensitivePayload_NeverLogsRequestPropertyValues()
    {
        // Arrange
        const string payload = "secret-payload-value";
        await using var provider = BuildCapturingHost();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        await sender.Send(new RegisterCustomerCommand(payload), TestContext.Current.CancellationToken);

        // Assert
        var entries = provider.GetRequiredService<LogSink>().Entries;
        entries.Should().NotBeEmpty();
        entries.SelectMany(entry => entry.Scopes.Append(entry.State).Append(entry.Message))
            .Select(Render).Should().NotContain(text => text.Contains(payload, StringComparison.Ordinal));
    }

    private static ServiceProvider BuildCapturingHost() => ApplicationTestHost.Build(services =>
    {
        services.AddSingleton<LogSink>();
        services.AddSingleton(typeof(ILogger<>), typeof(CapturingLogger<>));
    });

    private static string Render(object? value) => value switch
    {
        IEnumerable<KeyValuePair<string, object?>> pairs => string.Join(";", pairs.Select(pair => $"{pair.Key}={pair.Value}")),
        _ => value?.ToString() ?? string.Empty,
    };
}
