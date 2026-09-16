namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Infrastructure.Repositories;

/// <summary>
/// Deliberate violation: a horizontal <c>Repositories</c> folder is
/// banned — a repository belongs next to the aggregate it persists, not grouped by technical role.
/// </summary>
public sealed class LedgerEntryLegacyRepositoryStore;
