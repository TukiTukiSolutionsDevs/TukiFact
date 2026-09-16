using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class RegisterKernelTestOrderCommandHandler(IKernelTestOrderRepository orders, IClock clock)
    : IRequestHandler<RegisterKernelTestOrderCommand, Result>
{
    public Task<Result> Handle(RegisterKernelTestOrderCommand request, CancellationToken cancellationToken)
    {
        orders.Add(KernelTestOrder.Register(request.OrderId, request.TenantId, request.Reference, clock.UtcNow));
        return Task.FromResult(Result.Success());
    }
}
