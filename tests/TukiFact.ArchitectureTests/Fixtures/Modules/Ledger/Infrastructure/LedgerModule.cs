using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Presentation;
using TukiFact.Common.Infrastructure.Modules;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Infrastructure;

/// <summary>Clean fixture in the violating module: no reference to Presentation.</summary>
public sealed class LedgerAccountRepository;

/// <summary>
/// Deliberate violation: Infrastructure must never see endpoints —
/// the host registers the Presentation assembly next to the module, Infrastructure never maps one.
/// Also part of the composition violation below: implements <see cref="IModule"/>, but so does
/// <see cref="LedgerLegacyModule"/> — a module must have exactly one.
/// </summary>
public sealed class LedgerModule(GetLedgerAccountEndpoint endpoint) : IModule
{
    public string Name => "ledger";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        _ = endpoint;
        _ = services;
        _ = configuration;
    }
}

/// <summary>
/// Deliberate violation: a second <see cref="IModule"/> implementation
/// in the same module — exactly one is allowed, at <c>&lt;Module&gt;Module</c>.
/// </summary>
public sealed class LedgerLegacyModule : IModule
{
    public string Name => "ledger";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        _ = services;
        _ = configuration;
    }
}
