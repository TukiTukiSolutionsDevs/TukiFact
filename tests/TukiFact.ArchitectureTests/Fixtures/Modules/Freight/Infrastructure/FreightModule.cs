using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TukiFact.Common.Infrastructure.Modules;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Infrastructure;

/// <summary>Clean fixture: exactly one <see cref="IModule"/> implementation, named <c>FreightModule</c>, at the Infrastructure root.</summary>
public sealed class FreightModule : IModule
{
    public string Name => "freight";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        _ = services;
        _ = configuration;
    }
}
