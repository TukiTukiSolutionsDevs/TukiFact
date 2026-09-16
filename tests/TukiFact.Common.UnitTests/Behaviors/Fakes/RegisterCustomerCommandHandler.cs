using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class RegisterCustomerCommandHandler(CallLog log) : IRequestHandler<RegisterCustomerCommand, Result>
{
    public Task<Result> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        log.Add(nameof(RegisterCustomerCommandHandler));
        return Task.FromResult(Result.Success());
    }
}
