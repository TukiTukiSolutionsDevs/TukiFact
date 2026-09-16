using Microsoft.Extensions.Logging;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class KernelLogSinkProvider(KernelLogSink sink) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new KernelSinkLogger(categoryName, sink);

    public void Dispose()
    {
        // Nothing to release: the sink outlives the provider.
    }

    private sealed class KernelSinkLogger(string category, KernelLogSink sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            sink.Write(new KernelLogEntry(category, logLevel, eventId, formatter(state, exception), exception));
    }
}
