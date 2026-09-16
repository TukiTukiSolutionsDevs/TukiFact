using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class VoidInvoiceCommandHandler : IRequestHandler<VoidInvoiceCommand, Result>
{
    public static readonly Error InvoiceNotFound = Error.NotFound("Invoice.NotFound", "Invoice not found.");

    public Task<Result> Handle(VoidInvoiceCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure(InvoiceNotFound));
}
