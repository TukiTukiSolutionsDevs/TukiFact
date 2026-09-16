using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class RejectOrderCommandHandler : IRequestHandler<RejectOrderCommand, Result>
{
    public static readonly Error AlreadyShipped = Error.Conflict("Order.AlreadyShipped", "The order was already shipped.");

    public Task<Result> Handle(RejectOrderCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure(AlreadyShipped));
}
