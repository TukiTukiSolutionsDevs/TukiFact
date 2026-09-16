namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Application;

/// <summary>
/// Deliberate violation: a request (<c>*Command</c>/<c>*Query</c>)
/// must be <c>public sealed</c> — this one is only <c>internal</c>.
/// </summary>
internal sealed record ArchiveLedgerEntryCommand(Guid Id);
