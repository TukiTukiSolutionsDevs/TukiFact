namespace TukiFact.Common.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Current instant on the business wall clock (America/Lima, UTC-05:00).</summary>
    DateTimeOffset LimaNow { get; }
}
