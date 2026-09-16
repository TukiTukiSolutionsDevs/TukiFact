using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class PlaceBasketCommandHandler(CallLog log) : IRequestHandler<PlaceBasketCommand, Result>
{
    public Task<Result> Handle(PlaceBasketCommand request, CancellationToken cancellationToken)
    {
        log.Add(nameof(PlaceBasketCommandHandler));
        return Task.FromResult(Result.Success());
    }
}
