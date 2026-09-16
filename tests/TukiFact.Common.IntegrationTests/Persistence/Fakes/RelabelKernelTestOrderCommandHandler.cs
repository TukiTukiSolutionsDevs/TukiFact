using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class RelabelKernelTestOrderCommandHandler(IKernelTestOrderRepository orders, IClock clock)
    : IRequestHandler<RelabelKernelTestOrderCommand, Result>
{
    public static readonly Error NotFound = Error.NotFound("KernelTestOrder.NotFound", "Order was not found");

    public async Task<Result> Handle(RelabelKernelTestOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(NotFound);
        }

        order.Relabel(request.Reference, clock.UtcNow);
        return Result.Success();
    }
}
