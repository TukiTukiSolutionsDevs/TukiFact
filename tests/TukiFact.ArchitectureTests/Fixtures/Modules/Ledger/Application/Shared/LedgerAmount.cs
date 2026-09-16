namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Application.Shared;

/// <summary>
/// Deliberate violation: a top-level <c>Shared</c> folder directly
/// under the layer root is banned — <c>Shared</c> is only allowed nested inside a submodule folder.
/// </summary>
public sealed record LedgerAmount(decimal Value);
