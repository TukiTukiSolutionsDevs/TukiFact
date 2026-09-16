using TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Domain;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Application;

public sealed record GetLedgerEntryDetailQuery(Guid Id);

/// <summary>
/// Deliberate violation: a query handler must never use
/// <c>ITransactionManager</c> or a repository port directly — that is a write-side/Infrastructure
/// concern; the read side goes through a read connection instead.
/// </summary>
internal sealed class GetLedgerEntryDetailQueryHandler(ILedgerAccountRepository repository)
{
    public void Handle(GetLedgerEntryDetailQuery query)
    {
        _ = repository;
        _ = query;
    }
}
