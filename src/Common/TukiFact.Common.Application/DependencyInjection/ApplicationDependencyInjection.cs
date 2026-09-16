using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Behaviors;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Application.Time;
using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.Common.Application.DependencyInjection;

public static class ApplicationDependencyInjection
{
    private static readonly Type[] HandlerContracts =
    [
        typeof(IRequestHandler<,>),
        typeof(INotificationHandler<>),
        typeof(IDomainEventHandler<>),
    ];

    /// <summary>
    /// Registers the mediator, the pipeline behaviors (Logging → Telemetry → Validation → Transaction),
    /// the clock, and every handler and FluentValidation validator found in <paramref name="assemblies"/>.
    /// </summary>
    public static IServiceCollection AddCommonApplication(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.TryAddScoped<ISender, Mediator>();
        services.TryAddScoped<IPublisher, Mediator>();

        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(TelemetryBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>)));

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, Clock>();

        AddHandlers(services, assemblies);
        services.AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }

    private static void AddHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var implementations = assemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false });

        foreach (var implementation in implementations)
        {
            var contracts = implementation
                .GetInterfaces()
                .Where(contract => contract.IsGenericType && HandlerContracts.Contains(contract.GetGenericTypeDefinition()));

            foreach (var contract in contracts)
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, implementation));
            }
        }
    }
}
