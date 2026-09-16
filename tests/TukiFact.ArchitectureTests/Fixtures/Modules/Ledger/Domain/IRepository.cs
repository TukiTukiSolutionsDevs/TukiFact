namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Domain;

/// <summary>Deliberate violation: a generic <c>IRepository&lt;T&gt;</c> is banned.</summary>
public interface IRepository<T>
{
    void Add(T entity);
}

/// <summary>Deliberate violation: <c>IUnitOfWork</c> is banned — a transaction is scoped by <c>ITransactionManager</c>, not a unit of work.</summary>
public interface IUnitOfWork
{
    void Commit();
}
