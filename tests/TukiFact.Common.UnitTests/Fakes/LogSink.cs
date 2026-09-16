using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace TukiFact.Common.UnitTests.Fakes;

/// <summary>Collects every entry written through <see cref="CapturingLogger{T}"/>, with the scopes active at write time.</summary>
internal sealed class LogSink
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly AsyncLocal<ImmutableStack<object?>> _scopes = new();

    public IReadOnlyList<LogEntry> Entries => [.. _entries];

    public void Write(LogLevel level, EventId eventId, object? state, Exception? exception, string message) =>
        _entries.Enqueue(new LogEntry(level, eventId, state, exception, message, [.. Scopes]));

    public IDisposable Push(object? scope)
    {
        var previous = Scopes;
        _scopes.Value = previous.Push(scope);
        return new ScopePop(() => _scopes.Value = previous);
    }

    private ImmutableStack<object?> Scopes => _scopes.Value ?? ImmutableStack<object?>.Empty;

    private sealed class ScopePop(Action pop) : IDisposable
    {
        public void Dispose() => pop();
    }
}

internal sealed record LogEntry(
    LogLevel Level,
    EventId EventId,
    object? State,
    Exception? Exception,
    string Message,
    IReadOnlyList<object?> Scopes);
