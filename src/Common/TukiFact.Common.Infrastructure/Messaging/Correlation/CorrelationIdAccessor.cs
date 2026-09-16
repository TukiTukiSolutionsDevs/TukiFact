using System.Diagnostics;
using TukiFact.Common.Application.Abstractions;

namespace TukiFact.Common.Infrastructure.Messaging.Correlation;

/// <summary>Scoped holder; defaults to the current trace id (same value <c>LoggingBehavior</c> logs) or a new id.</summary>
internal sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private const int MaxLength = 128;

    private string? _correlationId;

    public string CorrelationId => _correlationId ??= Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N");

    public void Set(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        if (correlationId.Length > MaxLength)
        {
            throw new ArgumentException($"A correlation id has at most {MaxLength} characters.", nameof(correlationId));
        }

        _correlationId = correlationId;
    }
}
