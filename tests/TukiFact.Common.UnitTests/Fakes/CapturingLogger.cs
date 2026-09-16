using Microsoft.Extensions.Logging;

namespace TukiFact.Common.UnitTests.Fakes;

internal sealed class CapturingLogger<T>(LogSink sink) : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => sink.Push(state);

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        sink.Write(logLevel, eventId, state, exception, formatter(state, exception));
}
