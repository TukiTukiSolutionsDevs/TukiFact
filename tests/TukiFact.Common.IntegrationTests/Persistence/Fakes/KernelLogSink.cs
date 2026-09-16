using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>Collects every entry the kernel logs during the collection; tests filter by level, message or exception.</summary>
public sealed class KernelLogSink
{
    private readonly ConcurrentQueue<KernelLogEntry> _entries = new();

    public IReadOnlyList<KernelLogEntry> Entries => [.. _entries];

    public int Count(LogLevel level, string messageStart) =>
        Entries.Count(entry => entry.Level == level && entry.Message.StartsWith(messageStart, StringComparison.Ordinal));

    internal void Write(KernelLogEntry entry) => _entries.Enqueue(entry);
}

public sealed record KernelLogEntry(string Category, LogLevel Level, EventId EventId, string Message, Exception? Exception);
