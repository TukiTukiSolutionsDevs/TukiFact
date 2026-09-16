using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>
/// <paramref name="TenantId"/> is the tenant stamped on the new row — it is taken from the request, not from
/// <c>ICurrentUser</c>, precisely so a test can attempt writing a row for a different tenant than the one bound
/// to the transaction's GUC and prove the database's <c>WITH CHECK</c> clause rejects it, not application code.
/// </summary>
public sealed record RegisterKernelTestOrderCommand(Guid OrderId, Guid TenantId, string Reference) : IRequest<Result>;
