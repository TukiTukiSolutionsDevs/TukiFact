using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class CrashOrderCommandHandler : IRequestHandler<CrashOrderCommand, Result>
{
    public Task<Result> Handle(CrashOrderCommand request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Simulated handler crash.");
}
