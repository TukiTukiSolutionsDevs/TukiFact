using System.Collections.Concurrent;

namespace TukiFact.Common.UnitTests.Fakes;

/// <summary>Records the order handlers/behaviors actually ran in, shared across a single DI scope.</summary>
internal sealed class CallLog
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyList<string> Entries => [.. _entries];

    public void Add(string entry) => _entries.Enqueue(entry);
}
