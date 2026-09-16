using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Messaging;

/// <summary>
/// Cross-cutting step around a request handler. Behaviors run in registration order:
/// Logging → Telemetry → Validation → Transaction → Handler.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken);
}
