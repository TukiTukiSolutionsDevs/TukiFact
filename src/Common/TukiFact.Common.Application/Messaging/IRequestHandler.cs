using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Messaging;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
