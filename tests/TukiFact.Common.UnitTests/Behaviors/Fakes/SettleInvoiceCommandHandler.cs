using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class SettleInvoiceCommandHandler : IRequestHandler<SettleInvoiceCommand, Result>
{
    public async Task<Result> Handle(SettleInvoiceCommand request, CancellationToken cancellationToken)
    {
        await request.Cancellation.CancelAsync();
        return Result.Success();
    }
}
