using Microsoft.EntityFrameworkCore;
using TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Application;
using TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Domain;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Application;

public sealed record GetLedgerAccountQuery(Guid AccountId);

/// <summary>
/// Clean fixture in the violating module: depends on its own Domain only, internal sealed, next to
/// its request, no <c>ITransactionManager</c>/repository port.
/// </summary>
internal sealed class GetLedgerAccountQueryHandler
{
    public LedgerAccount Handle(GetLedgerAccountQuery query) => new() { Id = query.AccountId };
}

public sealed record PostLedgerEntryCommand(Guid AccountId);

/// <summary>
/// Deliberate multi-violation: an Application type must never
/// touch a data framework directly (<see cref="DbContext"/> — that is Infrastructure's job), a module
/// must never reference another module (<see cref="GetShipmentQuery"/> is Freight's), and a
/// handler must be <c>internal sealed</c> — this one is <c>public</c> (so it can expose a
/// same-visibility Freight type; the class itself carries the visibility violation).
/// </summary>
public sealed class PostLedgerEntryCommandHandler(DbContext dbContext, GetShipmentQuery otherModule)
{
    public void Handle(PostLedgerEntryCommand command)
    {
        _ = dbContext;
        _ = otherModule;
        _ = command;
    }
}
