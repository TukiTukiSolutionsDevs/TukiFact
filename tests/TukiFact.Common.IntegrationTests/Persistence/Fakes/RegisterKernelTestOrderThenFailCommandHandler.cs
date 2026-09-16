using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class RegisterKernelTestOrderThenFailCommandHandler(IKernelTestOrderRepository orders, IClock clock)
    : IRequestHandler<RegisterKernelTestOrderThenFailCommand, Result>
{
    public static readonly Error Rejected = Error.Conflict("KernelTestOrder.Rejected", "Order was rejected after registration");

    public Task<Result> Handle(RegisterKernelTestOrderThenFailCommand request, CancellationToken cancellationToken)
    {
        orders.Add(KernelTestOrder.Register(request.OrderId, request.TenantId, "rejected", clock.UtcNow));
        return Task.FromResult(Result.Failure(Rejected));
    }
}
