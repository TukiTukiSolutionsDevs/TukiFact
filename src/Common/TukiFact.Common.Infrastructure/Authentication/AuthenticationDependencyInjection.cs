using TukiFact.Common.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TukiFact.Common.Infrastructure.Authentication;

public static class AuthenticationDependencyInjection
{
    /// <summary>
    /// Registers <see cref="ICurrentUser"/> resolved from JWT claims only (never from request headers). Inert until
    /// the host calls it (host wiring is a later change): the kernel maps no endpoints yet, so nothing resolves
    /// this in production during this change.
    /// </summary>
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }
}
