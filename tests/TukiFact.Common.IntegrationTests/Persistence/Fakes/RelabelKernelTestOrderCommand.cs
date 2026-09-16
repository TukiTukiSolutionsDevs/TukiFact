using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

public sealed record RelabelKernelTestOrderCommand(Guid OrderId, string Reference) : IRequest<Result>;
