namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Domain;

/// <summary>
/// Deliberate double violation: a repository must expose at most 5
/// public methods (this one has 6) and must never expose <see cref="IQueryable{T}"/> (<see cref="Query"/> does).
/// </summary>
public interface ILedgerAccountRepository
{
    void Add(LedgerAccount account);

    void Remove(LedgerAccount account);

    void Update(LedgerAccount account);

    LedgerAccount? Find(Guid id);

    IEnumerable<LedgerAccount> FindAll();

    IQueryable<LedgerAccount> Query();
}
