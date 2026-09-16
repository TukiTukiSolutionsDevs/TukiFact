using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class ApproveInvoiceCommandHandler : IRequestHandler<ApproveInvoiceCommand, Result>
{
    public Task<Result> Handle(ApproveInvoiceCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}
