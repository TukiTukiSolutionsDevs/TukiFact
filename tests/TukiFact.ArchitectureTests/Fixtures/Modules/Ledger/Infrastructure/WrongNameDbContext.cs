using Microsoft.EntityFrameworkCore;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Infrastructure;

/// <summary>
/// Deliberate violation: a module has exactly one
/// <c>&lt;Module&gt;DbContext</c> — this one is named wrong (expected <c>LedgerDbContext</c>).
/// </summary>
public sealed class WrongNameDbContext : DbContext;
