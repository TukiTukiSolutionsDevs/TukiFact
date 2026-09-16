using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Messaging.Fakes;

internal sealed class GetOrderStatusQueryHandler : IRequestHandler<GetOrderStatusQuery, Result<string>>
{
    public const string Status = "Shipped";

    public Task<Result<string>> Handle(GetOrderStatusQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(Status));
}
