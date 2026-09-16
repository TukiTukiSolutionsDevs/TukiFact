using TukiFact.Common.Application.DependencyInjection;
using TukiFact.Common.Infrastructure.Modules;
using TukiFact.Common.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>The disposable fake module (ADR-007): proves the kernel end to end with zero production schema.</summary>
public sealed class KernelTestModule : IModule
{
    public string Name => KernelTestDbContext.SchemaName;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCommonApplication(typeof(KernelTestModule).Assembly);
        services.AddModuleDbContext<KernelTestDbContext>(KernelTestDbContext.SchemaName);
        services.AddScoped<IKernelTestOrderRepository, KernelTestOrderRepository>();
        services.AddReadDbConnection<IKernelTestOrderReadDbConnection, KernelTestOrderReadDbConnection>();
    }
}
