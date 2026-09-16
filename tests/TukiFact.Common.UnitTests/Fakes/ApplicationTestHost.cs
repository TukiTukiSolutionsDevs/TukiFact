using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.DependencyInjection;

namespace TukiFact.Common.UnitTests.Fakes;

/// <summary>Builds the real <c>AddCommonApplication</c> wiring with fakes for its external ports.</summary>
internal static class ApplicationTestHost
{
    public static ServiceProvider Build(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<CallLog>();
        services.AddSingleton<FakeTransactionManager>();
        services.AddSingleton<ITransactionManager>(provider => provider.GetRequiredService<FakeTransactionManager>());
        services.AddSingleton<ICurrentUser, FakeCurrentUser>();
        services.AddCommonApplication(typeof(ApplicationTestHost).Assembly);
        configure?.Invoke(services);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
    }
}
