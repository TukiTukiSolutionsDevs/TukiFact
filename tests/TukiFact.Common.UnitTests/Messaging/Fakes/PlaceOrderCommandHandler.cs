using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Messaging.Fakes;

internal sealed class PlaceOrderCommandHandler(CallLog log) : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        log.Add(nameof(PlaceOrderCommandHandler));
        return Task.FromResult(Result.Success(request.OrderId));
    }
}
