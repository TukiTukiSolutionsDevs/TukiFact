using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TukiFact.Common.Infrastructure.Modules;

/// <summary>
/// Composition contract of a module: implemented once by <c>&lt;Module&gt;Module</c> in the module Infrastructure
/// project and listed explicitly by the host. Endpoints live in the module Presentation project, which
/// Infrastructure cannot reference, so the host registers that assembly with <c>AddEndpoints</c> next to the module.
/// </summary>
public interface IModule
{
    /// <summary>Lower-case module name, also its Postgres schema (e.g. <c>identity</c>).</summary>
    string Name { get; }

    /// <summary>
    /// Registers the module: its Application assembly with <c>AddCommonApplication</c>, its DbContext with
    /// <c>AddModuleDbContext</c>, repositories, read connections and options. Called after the Common registrations.
    /// </summary>
    void Register(IServiceCollection services, IConfiguration configuration);
}
