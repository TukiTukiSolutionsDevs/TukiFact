namespace TukiFact.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string subject, T eventData, CancellationToken ct = default) where T : class;
}
