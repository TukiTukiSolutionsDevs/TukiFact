using Microsoft.EntityFrameworkCore;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Infrastructure;

/// <summary>Clean fixture: exactly one <c>&lt;Module&gt;DbContext</c> per module.</summary>
public sealed class FreightDbContext : DbContext;
