namespace TukiFact.Application.Interfaces;

/// <summary>
/// Handles a specific NATS JetStream event subject.
/// Each handler declares which subject(s) it handles and processes the message.
/// </summary>
public interface IEventHandler
{
    /// <summary>
    /// The NATS subject(s) this handler processes (e.g. "document.sent", "document.created").
    /// </summary>
    IReadOnlyList<string> Subjects { get; }

    /// <summary>
    /// Process the event message.
    /// </summary>
    /// <param name="subject">The actual subject that triggered the event</param>
    /// <param name="payload">Raw JSON payload as byte array</param>
    /// <param name="ct">Cancellation token</param>
    Task HandleAsync(string subject, byte[] payload, CancellationToken ct);
}
