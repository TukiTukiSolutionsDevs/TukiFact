using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class RegisterKernelTestOrderThenCrashCommandHandler(IKernelTestOrderRepository orders, IClock clock)
    : IRequestHandler<RegisterKernelTestOrderThenCrashCommand, Result>
{
    public Task<Result> Handle(RegisterKernelTestOrderThenCrashCommand request, CancellationToken cancellationToken)
    {
        orders.Add(KernelTestOrder.Register(request.OrderId, request.TenantId, "crashed", clock.UtcNow));
        return Task.FromException<Result>(new InvalidOperationException("Handler crashed after registration."));
    }
}
