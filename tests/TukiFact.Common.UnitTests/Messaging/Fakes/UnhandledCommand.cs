using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Messaging.Fakes;

/// <summary>Deliberately has no registered <see cref="IRequestHandler{TRequest,TResponse}"/>.</summary>
internal sealed record UnhandledCommand : IRequest;
