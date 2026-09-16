namespace TukiFact.Common.Application.Abstractions;

/// <summary>
/// Correlation id of the current scope (HTTP request or consumed NATS JetStream message). Middleware sets it from
/// <see cref="HeaderName"/>; when nobody set it, the current trace id is used.
/// </summary>
public interface ICorrelationIdAccessor
{
    const string HeaderName = "X-Correlation-Id";

    string CorrelationId { get; }

    void Set(string correlationId);
}
