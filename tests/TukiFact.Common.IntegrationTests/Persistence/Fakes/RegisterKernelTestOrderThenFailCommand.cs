using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

public sealed record RegisterKernelTestOrderThenFailCommand(Guid OrderId, Guid TenantId) : IRequest<Result>;
