using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class SearchCustomersQueryHandler(CallLog log) : IRequestHandler<SearchCustomersQuery, Result<IReadOnlyList<string>>>
{
    public Task<Result<IReadOnlyList<string>>> Handle(SearchCustomersQuery request, CancellationToken cancellationToken)
    {
        log.Add(nameof(SearchCustomersQueryHandler));
        return Task.FromResult(Result.Success<IReadOnlyList<string>>([]));
    }
}
