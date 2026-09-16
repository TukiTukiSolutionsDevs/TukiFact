using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Messaging.Fakes;

internal sealed record GetOrderStatusQuery(Guid OrderId) : IRequest<Result<string>>;
