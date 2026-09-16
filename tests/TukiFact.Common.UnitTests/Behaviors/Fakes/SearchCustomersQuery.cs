using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed record SearchCustomersQuery(int PageSize) : IRequest<Result<IReadOnlyList<string>>>;
